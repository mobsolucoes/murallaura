using HashtagWall.Domain.Entities;

namespace HashtagWall.Application.Interfaces;

public interface IHashtagConfigurationRepository
{
    Task<IReadOnlyList<HashtagConfiguration>> GetAllAsync(CancellationToken ct);
    Task<HashtagConfiguration?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<HashtagConfiguration?> GetByNormalizedHashtagAsync(string normalized, CancellationToken ct);
    Task<HashtagConfiguration> AddAsync(HashtagConfiguration entity, CancellationToken ct);
    Task UpdateAsync(HashtagConfiguration entity, CancellationToken ct);
}
