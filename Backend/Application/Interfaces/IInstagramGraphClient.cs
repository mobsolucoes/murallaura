namespace HashtagWall.Application.Interfaces;

public interface IInstagramGraphClient
{
    Task<string?> ResolveHashtagIdAsync(string accessToken, string instagramUserId, string hashtagWithoutHash, CancellationToken ct);
    Task<IReadOnlyList<InstagramMediaSnapshot>> GetRecentHashtagMediaAsync(string accessToken, string instagramUserId, string hashtagId, CancellationToken ct);
}

public record InstagramMediaSnapshot(
    string Id,
    string? Caption,
    string? MediaUrl,
    string? ThumbnailUrl,
    string Permalink,
    string MediaType,
    DateTimeOffset Timestamp);
