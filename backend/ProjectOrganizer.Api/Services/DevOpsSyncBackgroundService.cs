namespace ProjectOrganizer.Api.Services;

public class DevOpsSyncBackgroundService : BackgroundService
{
    private static readonly TimeOnly DailyRunAt = new(7, 0);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DevOpsSyncBackgroundService> _logger;
    private readonly TimeZoneInfo _timeZone;

    public DevOpsSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DevOpsSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeZone = ResolveBelgradeTimeZone();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DevOpsSyncBackgroundService pokrenut. Dnevno osvežavanje je zakazano u {Time} ({TimeZone}).",
            DailyRunAt,
            _timeZone.Id);

        await RunMissedSyncIfNeededAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            _logger.LogInformation("Sledeće DevOps osvežavanje je za {Delay}.", delay);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunDailySyncAsync(stoppingToken);
        }
    }

    private async Task RunMissedSyncIfNeededAsync(CancellationToken stoppingToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        if (!HasTodayRunTimePassed(utcNow, out var localTodayRunUtc))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<DevOpsSyncService>();

            var alreadySyncedToday = await syncService.HasLinkedTaskSyncSinceAsync(localTodayRunUtc, stoppingToken);
            if (alreadySyncedToday)
                return;

            _logger.LogInformation("DevOps osvežavanje za danas nije zabeleženo nakon 07:00. Pokrećem catch-up sync.");
            await RunDailySyncAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška tokom catch-up DevOps osvežavanja.");
        }
    }

    private async Task RunDailySyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<DevOpsSyncService>();

            var result = await syncService.RefreshAllLinkedTasksAsync(stoppingToken);

            _logger.LogInformation(
                "DevOps dnevno osvežavanje završeno. Total={Total}, Refreshed={Refreshed}, Skipped={Skipped}, Failed={Failed}",
                result.TotalTasks,
                result.RefreshedTasks,
                result.SkippedTasks,
                result.FailedTasks);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška tokom dnevnog DevOps osvežavanja.");
        }
    }

    private TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var nextLocalRun = localNow.Date.Add(DailyRunAt.ToTimeSpan());

        if (localNow.DateTime >= nextLocalRun)
        {
            nextLocalRun = nextLocalRun.AddDays(1);
        }

        var nextRunUnspecified = DateTime.SpecifyKind(nextLocalRun, DateTimeKind.Unspecified);
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunUnspecified, _timeZone);

        return nextRunUtc - utcNow.UtcDateTime;
    }

    private bool HasTodayRunTimePassed(DateTimeOffset utcNow, out DateTime localTodayRunUtc)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var localTodayStart = localNow.Date;
        var todayRun = localTodayStart.Add(DailyRunAt.ToTimeSpan());

        var todayRunUnspecified = DateTime.SpecifyKind(todayRun, DateTimeKind.Unspecified);
        localTodayRunUtc = TimeZoneInfo.ConvertTimeToUtc(todayRunUnspecified, _timeZone);

        return localNow.DateTime >= todayRun;
    }

    private static TimeZoneInfo ResolveBelgradeTimeZone()
    {
        foreach (var id in new[] { "Europe/Belgrade", "Central European Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
