using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;

namespace ProjectOrganizer.Api.Services;

/// <summary>
/// Pozadinski servis koji periodično proverava projekte i generiše reminder notifikacije.
/// Pokreće se jednom na sat. Koristi IServiceScopeFactory da kreira scoped servise
/// (DbContext, NotificationService) unutar singleton background servisa.
///
/// Pravila koja se proveravaju:
///   - Blocked:              Projekat je u statusu "Blocked" duže od 7 dana
///   - Inactive:             Aktivan projekat nema aktivnosti poslednjih 14 dana
///   - DeadlineApproaching:  Aktivnost projekta ističe u sledećih 7 dana
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationBackgroundService> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    // Konfigurabilni pragovi – lako proširivo u appsettings.json u budućnosti
    private const int BlockedThresholdDays      = 7;
    private const int InactiveThresholdDays     = 14;
    private const int DeadlineApproachingDays   = 7;

    public NotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationBackgroundService pokrenut.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunChecksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška u NotificationBackgroundService.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunChecksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context             = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var today               = DateTime.UtcNow.Date;

        await CheckBlockedProjectsAsync(context, notificationService, today);
        await CheckInactiveProjectsAsync(context, notificationService, today);
        await CheckDeadlineApproachingAsync(context, notificationService, today);
        await CheckAiRemindersAsync(context, notificationService);
    }

    // -----------------------------------------------------------------
    // Pravilo 1: Projekat je u statusu "Blocked" duže od N dana
    // -----------------------------------------------------------------
    private async Task CheckBlockedProjectsAsync(
        ApplicationDbContext context,
        NotificationService notificationService,
        DateTime today)
    {
        var threshold = today.AddDays(-BlockedThresholdDays);

        var projects = await context.Projekti
            .Where(p => p.AIPracen && p.Aktivan && p.Status == "Blocked" && p.UpdatedAt < threshold)
            .Select(p => new { p.Id, p.Naziv, p.CreatedBy })
            .ToListAsync();

        foreach (var p in projects)
        {
            if (p.CreatedBy == null) continue;

            await notificationService.CreateAndSendAsync(
                userId:       p.CreatedBy.Value,
                type:         "Blocked",
                message:      $"Projekat \"{p.Naziv}\" je u statusu Blocked već {BlockedThresholdDays}+ dana.",
                projekatId:   p.Id,
                referenceKey: $"Blocked:{p.Id}:{today:yyyy-MM-dd}");
        }
    }

    // -----------------------------------------------------------------
    // Pravilo 2: Aktivan projekat nema aktivnosti duže od N dana
    // -----------------------------------------------------------------
    private async Task CheckInactiveProjectsAsync(
        ApplicationDbContext context,
        NotificationService notificationService,
        DateTime today)
    {
        var threshold = today.AddDays(-InactiveThresholdDays);

        var projects = await context.Projekti
            .Where(p => p.AIPracen && p.Aktivan && p.Status != "Blocked" && p.Status != "Completed")
            .Where(p => !p.Aktivnosti.Any(a => a.Datum >= threshold))
            .Select(p => new { p.Id, p.Naziv, p.CreatedBy })
            .ToListAsync();

        foreach (var p in projects)
        {
            if (p.CreatedBy == null) continue;

            await notificationService.CreateAndSendAsync(
                userId:       p.CreatedBy.Value,
                type:         "Inactive",
                message:      $"Projekat \"{p.Naziv}\" nema aktivnosti poslednjih {InactiveThresholdDays} dana.",
                projekatId:   p.Id,
                referenceKey: $"Inactive:{p.Id}:{today:yyyy-MM-dd}");
        }
    }

    // -----------------------------------------------------------------
    // Pravilo 3: Projekat ima aktivnost čiji rok ističe uskoro
    // -----------------------------------------------------------------
    private async Task CheckDeadlineApproachingAsync(
        ApplicationDbContext context,
        NotificationService notificationService,
        DateTime today)
    {
        var deadlineWindow = today.AddDays(DeadlineApproachingDays);

        var projects = await context.Projekti
            .Where(p => p.AIPracen && p.Aktivan && p.Status != "Completed")
            .Where(p => p.Aktivnosti.Any(a =>
                a.EndUtc != null &&
                a.EndUtc >= today &&
                a.EndUtc <= deadlineWindow))
            .Select(p => new { p.Id, p.Naziv, p.CreatedBy })
            .ToListAsync();

        foreach (var p in projects)
        {
            if (p.CreatedBy == null) continue;

            await notificationService.CreateAndSendAsync(
                userId:       p.CreatedBy.Value,
                type:         "DeadlineApproaching",
                message:      $"Projekat \"{p.Naziv}\" ima aktivnosti koje ističu u sledećih {DeadlineApproachingDays} dana.",
                projekatId:   p.Id,
                referenceKey: $"DeadlineApproaching:{p.Id}:{today:yyyy-MM-dd}");
        }
    }

    // -----------------------------------------------------------------
    // Pravilo 4: AI podsetnike iz teksta aktivnosti (scan-on-write)
    // -----------------------------------------------------------------
    private async Task CheckAiRemindersAsync(
        ApplicationDbContext context,
        NotificationService notificationService)
    {
        var now = DateTime.UtcNow;
        var dueReminders = await context.AiReminders
            .Where(r => !r.Sent && r.RemindAt <= now)
            .ToListAsync();

        foreach (var reminder in dueReminders)
        {
            await notificationService.CreateAndSendAsync(
                userId:       reminder.UserId,
                type:         "AiReminder",
                message:      reminder.Message,
                projekatId:   reminder.ProjekatId,
                referenceKey: $"AiReminder:{reminder.Id}");

            reminder.Sent = true;
        }

        if (dueReminders.Count > 0)
            await context.SaveChangesAsync();
    }
}
