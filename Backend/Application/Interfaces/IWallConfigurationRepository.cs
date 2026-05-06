using HashtagWall.Domain.Entities;

namespace HashtagWall.Application.Interfaces;

public interface IWallConfigurationRepository
{
    Task<WallConfiguration?> GetByHashtagConfigIdAsync(Guid hashtagConfigurationId, CancellationToken ct);
    Task<WallConfiguration> UpsertAsync(WallConfiguration entity, CancellationToken ct);
}
