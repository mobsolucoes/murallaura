namespace HashtagWall.Application.Interfaces;

public interface IInstagramSyncService
{
    /// <summary>Sync one hashtag configuration; returns count of newly stored media.</summary>
    Task<int> SyncHashtagAsync(Guid hashtagConfigurationId, CancellationToken ct);

    Task SyncAllEnabledAsync(CancellationToken ct);
}
