using HashtagWall.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace HashtagWall.Worker;

/// <summary>Polls Instagram Graph API based on each hashtag's configured interval.</summary>
public sealed class InstagramPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InstagramPollingWorker> _logger;
    private readonly IOptionsMonitor<InstagramWebRpaOptions> _rpaOptions;

    public InstagramPollingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InstagramPollingWorker> logger,
        IOptionsMonitor<InstagramWebRpaOptions> rpaOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _rpaOptions = rpaOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        _logger.LogInformation("Instagram polling worker started. Tick interval: 30s.");

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var hashtags = scope.ServiceProvider.GetRequiredService<IHashtagConfigurationRepository>();
                var sync = scope.ServiceProvider.GetRequiredService<IInstagramSyncService>();
                var rpaSync = scope.ServiceProvider.GetRequiredService<InstagramWebRpaSyncService>();

                var list = await hashtags.GetAllAsync(stoppingToken);
                var now = DateTimeOffset.UtcNow;
                var useRpa = _rpaOptions.CurrentValue.Enabled;
                _logger.LogInformation(
                    "Polling tick at {Now}. Mode: {Mode}. Total hashtags: {Count}.",
                    now,
                    useRpa ? "InstagramWebRpa" : "GraphApi",
                    list.Count);

                foreach (var h in list.Where(x => x.IsMonitoringEnabled))
                {
                    var interval = TimeSpan.FromMinutes(Math.Clamp(h.PollIntervalMinutes, 1, 24 * 60));
                    if (h.LastSyncedAt is null || now - h.LastSyncedAt >= interval)
                    {
                        _logger.LogInformation(
                            "Starting sync for #{Tag}. LastSyncedAt: {LastSyncedAt}. IntervalMinutes: {Interval}.",
                            h.NormalizedHashtag,
                            h.LastSyncedAt,
                            interval.TotalMinutes);
                        var inserted = useRpa
                            ? await rpaSync.SyncHashtagAsync(h.Id, stoppingToken)
                            : await sync.SyncHashtagAsync(h.Id, stoppingToken);
                        _logger.LogInformation("Finished sync for #{Tag}: +{Inserted} new post(s).", h.NormalizedHashtag, inserted);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Skipping #{Tag}. Next run in ~{RemainingSeconds}s.",
                            h.NormalizedHashtag,
                            Math.Max(0, (interval - (now - h.LastSyncedAt.Value)).TotalSeconds));
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Instagram polling worker cancellation requested.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling iteration failed.");
            }
        }

        _logger.LogInformation("Instagram polling worker stopped.");
    }
}
