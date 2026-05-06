using HashtagWall.Domain.Enums;

namespace HashtagWall.Domain.Entities;

public class IntegrationLog
{
    public Guid Id { get; set; }

    public Guid? HashtagConfigurationId { get; set; }
    public HashtagConfiguration? HashtagConfiguration { get; set; }

    public IntegrationLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
