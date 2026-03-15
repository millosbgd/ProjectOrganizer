using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Hubs;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Sprema notifikaciju u bazu i šalje je korisniku u realnom vremenu putem SignalR-a.
    /// Ako korisnik nije online, notifikacija ostaje u bazi i biće dostupna pri sledećem učitavanju.
    /// </summary>
    /// <param name="userId">Interni ID korisnika (iz tabele Users)</param>
    /// <param name="type">Tip notifikacije (StatusChange, Blocked, Inactive, DeadlineApproaching)</param>
    /// <param name="message">Poruka notifikacije</param>
    /// <param name="projekatId">Opcionalni ID projekta</param>
    /// <param name="referenceKey">Ključ za dedupliciju – isti ključ neće generisati novu notifikaciju</param>
    public async Task CreateAndSendAsync(
        int userId,
        string type,
        string message,
        int? projekatId = null,
        string? referenceKey = null,
        int? aktivnostId = null)
    {
        // --- Deduplicija ---
        // Ako već postoji nepročitana notifikacija sa istim referenceKey, preskočiti.
        if (referenceKey != null)
        {
            var alreadyExists = await _context.Notifications
                .AnyAsync(n => n.ReferenceKey == referenceKey && !n.IsRead);

            if (alreadyExists)
            {
                _logger.LogDebug("Notifikacija preskočena (duplikat): {ReferenceKey}", referenceKey);
                return;
            }
        }

        // --- Korak 1: Snimi u bazu ---
        var notification = new Notification
        {
            UserId = userId,
            ProjekatId = projekatId,
            AktivnostId = aktivnostId,
            Type = type,
            Message = message,
            ReferenceKey = referenceKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // --- Korak 2: Pošalji u realnom vremenu ---
        // Neuspeh slanja je nebitan – notifikacija je već sačuvana u bazi.
        try
        {
            var auth0Id = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Auth0Id)
                .FirstOrDefaultAsync();

            if (auth0Id != null)
            {
                var payload = new
                {
                    notification.Id,
                    notification.Type,
                    notification.Message,
                    notification.ProjekatId,
                    notification.AktivnostId,
                    notification.IsRead,
                    notification.CreatedAt
                };

                await _hubContext.Clients.User(auth0Id).SendAsync("ReceiveNotification", payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR slanje nije uspelo za notifikaciju {Id}. Notifikacija je sačuvana u bazi.", notification.Id);
        }
    }
}
