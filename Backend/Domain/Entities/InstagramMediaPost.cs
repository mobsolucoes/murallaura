using HashtagWall.Domain.Enums;

namespace HashtagWall.Domain.Entities;

public class InstagramMediaPost
{
    public Guid Id { get; set; }

    public Guid HashtagConfigurationId { get; set; }
    public HashtagConfiguration HashtagConfiguration { get; set; } = null!;

    public string InstagramMediaId { get; set; } = string.Empty;

    /// <summary>Denormalized hashtag text for queries.</summary>
    public string Hashtag { get; set; } = string.Empty;

    public string? Caption { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Permalink { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public MediaPostStatus Status { get; set; } = MediaPostStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
