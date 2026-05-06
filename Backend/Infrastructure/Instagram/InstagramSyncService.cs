using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Domain.Enums;
using HashtagWall.Infrastructure.Services;

namespace HashtagWall.Infrastructure.Instagram;

public class InstagramSyncService : IInstagramSyncService
{
    private readonly IHashtagConfigurationRepository _hashtagRepo;
    private readonly IInstagramMediaRepository _mediaRepo;
    private readonly IIntegrationLogRepository _logRepo;
    private readonly IInstagramGraphClient _graph;

    public InstagramSyncService(
        IHashtagConfigurationRepository hashtagRepo,
        IInstagramMediaRepository mediaRepo,
        IIntegrationLogRepository logRepo,
        IInstagramGraphClient graph)
    {
        _hashtagRepo = hashtagRepo;
        _mediaRepo = mediaRepo;
        _logRepo = logRepo;
        _graph = graph;
    }

    public async Task<int> SyncHashtagAsync(Guid hashtagConfigurationId, CancellationToken ct)
    {
        var cfg = await _hashtagRepo.GetByIdAsync(hashtagConfigurationId, ct);
        if (cfg is null || !cfg.IsMonitoringEnabled || string.IsNullOrWhiteSpace(cfg.MetaAccessToken))
            return 0;

        var normalized = cfg.NormalizedHashtag;
        var hashtagLabel = $"#{normalized}";

        try
        {
            var hashtagId =
                await _graph.ResolveHashtagIdAsync(cfg.MetaAccessToken, cfg.InstagramBusinessAccountId, normalized, ct);
            if (string.IsNullOrEmpty(hashtagId))
            {
                await LogAsync(hashtagConfigurationId,
                    IntegrationLogLevel.Warning,
                    $"Hashtag id not returned for {hashtagLabel}. Check permissions (instagram_manage_insights / required scopes) and Business account.",
                    null,
                    ct);
                cfg.LastSyncedAt = DateTimeOffset.UtcNow;
                cfg.UpdatedAt = DateTimeOffset.UtcNow;
                await _hashtagRepo.UpdateAsync(cfg, ct);
                return 0;
            }

            var items = await _graph.GetRecentHashtagMediaAsync(cfg.MetaAccessToken, cfg.InstagramBusinessAccountId,
                hashtagId, ct);

            var inserted = 0;
            foreach (var snap in items)
            {
                if (await _mediaRepo.ExistsByInstagramMediaIdAsync(cfg.Id, snap.Id, ct))
                    continue;

                var primaryUrl = snap.MediaUrl ?? snap.ThumbnailUrl;
                if (string.IsNullOrWhiteSpace(primaryUrl))
                {
                    await LogAsync(hashtagConfigurationId, IntegrationLogLevel.Warning,
                        $"Skipping media {snap.Id} — no display URL.", snap.Permalink, ct);
                    continue;
                }

                var entity = new InstagramMediaPost
                {
                    Id = Guid.NewGuid(),
                    HashtagConfigurationId = cfg.Id,
                    InstagramMediaId = snap.Id,
                    Hashtag = normalized,
                    Caption = snap.Caption,
                    MediaUrl = snap.MediaUrl,
                    ThumbnailUrl = snap.ThumbnailUrl,
                    Permalink = snap.Permalink,
                    MediaType = snap.MediaType,
                    Timestamp = snap.Timestamp,
                    Status = cfg.AutoApprovePosts ? MediaPostStatus.Approved : MediaPostStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                await _mediaRepo.AddAsync(entity, ct);
                inserted++;
            }

            cfg.LastSyncedAt = DateTimeOffset.UtcNow;
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            await _hashtagRepo.UpdateAsync(cfg, ct);

            await LogAsync(hashtagConfigurationId, IntegrationLogLevel.Information,
                $"Sync OK for {hashtagLabel}: {inserted} new media (batch size {items.Count}).", null, ct);

            return inserted;
        }
        catch (GraphApiException ex)
        {
            await LogAsync(hashtagConfigurationId, IntegrationLogLevel.Error,
                $"Instagram Graph API: {ex.Message}",
                ex.RawBody,
                ct);

            cfg.LastSyncedAt = DateTimeOffset.UtcNow;
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            await _hashtagRepo.UpdateAsync(cfg, ct);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            await LogAsync(hashtagConfigurationId, IntegrationLogLevel.Error,
                $"HTTP error during Instagram sync: {ex.Message}",
                null,
                ct);

            cfg.LastSyncedAt = DateTimeOffset.UtcNow;
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            await _hashtagRepo.UpdateAsync(cfg, ct);
            return 0;
        }
        catch (Exception ex)
        {
            await LogAsync(hashtagConfigurationId, IntegrationLogLevel.Error,
                $"Unexpected sync error: {ex.Message}",
                ex.ToString(),
                ct);

            cfg.LastSyncedAt = DateTimeOffset.UtcNow;
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            await _hashtagRepo.UpdateAsync(cfg, ct);
            return 0;
        }
    }

    public async Task SyncAllEnabledAsync(CancellationToken ct)
    {
        var list = await _hashtagRepo.GetAllAsync(ct);
        foreach (var cfg in list.Where(h => h.IsMonitoringEnabled))
        {
            await SyncHashtagAsync(cfg.Id, ct);
        }
    }

    private async Task LogAsync(Guid? hashtagConfigurationId, IntegrationLogLevel level, string message,
        string? details, CancellationToken ct)
    {
        await _logRepo.AddAsync(new IntegrationLog
        {
            Id = Guid.NewGuid(),
            HashtagConfigurationId = hashtagConfigurationId,
            Level = level,
            Message = message,
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }
}
