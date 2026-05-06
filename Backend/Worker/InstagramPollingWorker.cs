using HashtagWall.Application.Interfaces;

namespace HashtagWall.Worker;

/// <summary>Polls Instagram Graph API based on each hashtag's configured interval.</summary>
public sealed class InstagramPollingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InstagramPollingWorker> _logger;

    public InstagramPollingWorker(IServiceScopeFactory scopeFactory, ILogger<InstagramPollingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var hashtags = scope.ServiceProvider.GetRequiredService<IHashtagConfigurationRepository>();
                var sync = scope.ServiceProvider.GetRequiredService<IInstagramSyncService>();

                var list = await hashtags.GetAllAsync(stoppingToken);
                var now = DateTimeOffset.UtcNow;

                foreach (var h in list.Where(x => x.IsMonitoringEnabled))
                {
                    var interval = TimeSpan.FromMinutes(Math.Clamp(h.PollIntervalMinutes, 1, 24 * 60));
                    if (h.LastSyncedAt is null || now - h.LastSyncedAt >= interval)
                    {
                        var inserted = await sync.SyncHashtagAsync(h.Id, stoppingToken);
                        if (inserted > 0)
                            _logger.LogInformation("Synced #{Tag}: +{Inserted} post(s).", h.NormalizedHashtag, inserted);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polling iteration failed.");
            }
        }
    }
}
