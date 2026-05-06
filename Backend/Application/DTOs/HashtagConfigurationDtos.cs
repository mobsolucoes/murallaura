namespace HashtagWall.Application.DTOs;

public record HashtagConfigurationDto(
    Guid Id,
    string Hashtag,
    string InstagramBusinessAccountId,
    bool MetaTokenConfigured,
    int PollIntervalMinutes,
    bool AutoApprovePosts,
    bool IsMonitoringEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record UpsertHashtagConfigurationRequest(
    string Hashtag,
    string InstagramBusinessAccountId,
    string? MetaAccessToken,
    int PollIntervalMinutes,
    bool AutoApprovePosts,
    bool IsMonitoringEnabled);
