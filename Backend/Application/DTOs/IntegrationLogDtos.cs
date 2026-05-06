using HashtagWall.Domain.Enums;

namespace HashtagWall.Application.DTOs;

public record IntegrationLogDto(
    Guid Id,
    Guid? HashtagConfigurationId,
    IntegrationLogLevel Level,
    string Message,
    string? Details,
    DateTimeOffset CreatedAt);
