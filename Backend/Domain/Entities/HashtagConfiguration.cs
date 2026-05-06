namespace HashtagWall.Domain.Entities;

/// <summary>
/// Instagram hashtag monitoring configuration (Meta token stored server-side only).
/// </summary>
public class HashtagConfiguration
{
    public Guid Id { get; set; }

    /// <summary>Normalized hashtag without # (e.g. minhaevento).</summary>
    public string NormalizedHashtag { get; set; } = string.Empty;

    /// <summary>Instagram Business/Creator scoped user id (IG User ID) used in Graph API calls.</summary>
    public string InstagramBusinessAccountId { get; set; } = string.Empty;

    /// <summary>Long-lived user or page access token with instagram_basic, pages_read_engagement, etc.</summary>
    public string MetaAccessToken { get; set; } = string.Empty;

    /// <summary>Minutes between sync attempts.</summary>
    public int PollIntervalMinutes { get; set; } = 5;

    public bool IsMonitoringEnabled { get; set; }
    public bool AutoApprovePosts { get; set; }

    /// <summary>Last successful sync attempt (used by worker scheduling).</summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public WallConfiguration? Wall { get; set; }
    public ICollection<InstagramMediaPost> MediaPosts { get; set; } = new List<InstagramMediaPost>();
    public ICollection<IntegrationLog> IntegrationLogs { get; set; } = new List<IntegrationLog>();
}
