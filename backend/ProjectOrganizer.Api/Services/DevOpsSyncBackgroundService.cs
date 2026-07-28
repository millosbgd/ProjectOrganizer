namespace ProjectOrganizer.Api.Services;

public class DevOpsSyncBackgroundService : BackgroundService
{
    private static readonly TimeOnly FirstRunAt = new(7, 0);
    private static readonly TimeOnly LastRunAt = new(17, 0);

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
            "DevOpsSyncBackgroundService pokrenut. Osvežavanje je zakazano svakog sata od {FirstRunAt} do {LastRunAt} ({TimeZone}).",
            FirstRunAt,
            LastRunAt,
            _timeZone.Id);

        await RunMissedSyncIfNeededAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow);
            _logger.LogInformation("Sledeće DevOps osvežavanje je za {Delay}.", delay);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunScheduledSyncAsync(stoppingToken);
        }
    }

    private async Task RunMissedSyncIfNeededAsync(CancellationToken stoppingToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        if (!TryGetLatestScheduledRunUtc(utcNow, out var latestScheduledRunUtc))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<DevOpsSyncService>();

            var alreadySyncedToday = await syncService.HasLinkedTaskSyncSinceAsync(latestScheduledRunUtc, stoppingToken);
            if (alreadySyncedToday)
                return;

            _logger.LogInformation("DevOps osvežavanje nije zabeleženo nakon poslednjeg planiranog termina. Pokrećem catch-up sync.");
            await RunScheduledSyncAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška tokom catch-up DevOps osvežavanja.");
        }
    }

    private async Task RunScheduledSyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<DevOpsSyncService>();

            var result = await syncService.RefreshAllLinkedTasksAsync(stoppingToken);

            _logger.LogInformation(
                "DevOps osvežavanje završeno. Total={Total}, Refreshed={Refreshed}, Skipped={Skipped}, Failed={Failed}",
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
            _logger.LogError(ex, "Greška tokom DevOps osvežavanja.");
        }
    }

    private TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var nextLocalRun = GetNextLocalRun(localNow.DateTime);
        var nextRunUnspecified = DateTime.SpecifyKind(nextLocalRun, DateTimeKind.Unspecified);
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunUnspecified, _timeZone);

        return nextRunUtc - utcNow.UtcDateTime;
    }

    private static DateTime GetNextLocalRun(DateTime localNow)
    {
        var firstRunToday = localNow.Date.Add(FirstRunAt.ToTimeSpan());
        var lastRunToday = localNow.Date.Add(LastRunAt.ToTimeSpan());

        if (localNow < firstRunToday)
            return firstRunToday;

        if (localNow >= lastRunToday)
            return firstRunToday.AddDays(1);

        return new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            localNow.Minute == 0 && localNow.Second == 0 && localNow.Millisecond == 0 ? localNow.Hour : localNow.Hour + 1,
            0,
            0);
    }

    private bool TryGetLatestScheduledRunUtc(DateTimeOffset utcNow, out DateTime latestScheduledRunUtc)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var firstRunToday = localNow.Date.Add(FirstRunAt.ToTimeSpan());

        if (localNow.DateTime < firstRunToday)
        {
            latestScheduledRunUtc = default;
            return false;
        }

        var lastRunToday = localNow.Date.Add(LastRunAt.ToTimeSpan());
        var latestLocalRun = localNow.DateTime >= lastRunToday
            ? lastRunToday
            : new DateTime(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, 0, 0);

        var latestRunUnspecified = DateTime.SpecifyKind(latestLocalRun, DateTimeKind.Unspecified);
        latestScheduledRunUtc = TimeZoneInfo.ConvertTimeToUtc(latestRunUnspecified, _timeZone);

        return true;
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
