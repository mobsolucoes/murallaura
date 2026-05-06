using HashtagWall.Domain.Enums;

namespace HashtagWall.Application.DTOs;

public record InstagramMediaPostDto(
    Guid Id,
    string InstagramMediaId,
    string Hashtag,
    string? Caption,
    string? MediaUrl,
    string? ThumbnailUrl,
    string Permalink,
    string MediaType,
    DateTimeOffset Timestamp,
    MediaPostStatus Status,
    DateTimeOffset CreatedAt);

public record DashboardStatsDto(
    int TotalCaptured,
    int Approved,
    int Pending,
    int Rejected);
