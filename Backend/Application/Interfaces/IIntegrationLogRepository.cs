using HashtagWall.Domain.Entities;

namespace HashtagWall.Application.Interfaces;

public interface IIntegrationLogRepository
{
    Task AddAsync(IntegrationLog log, CancellationToken ct);
    Task<IReadOnlyList<IntegrationLog>> ListRecentAsync(Guid? hashtagConfigurationId, int take, CancellationToken ct);
}
