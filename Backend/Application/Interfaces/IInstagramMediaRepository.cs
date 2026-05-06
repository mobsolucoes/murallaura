using HashtagWall.Domain.Entities;

namespace HashtagWall.Application.Interfaces;

public interface IInstagramMediaRepository
{
    Task<IReadOnlyList<InstagramMediaPost>> ListByHashtagAsync(Guid hashtagConfigId, CancellationToken ct);
    Task<IReadOnlyList<InstagramMediaPost>> ListApprovedByHashtagAsync(Guid hashtagConfigId, CancellationToken ct);
    Task<InstagramMediaPost?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByInstagramMediaIdAsync(Guid hashtagConfigId, string instagramMediaId, CancellationToken ct);
    Task<InstagramMediaPost> AddAsync(InstagramMediaPost entity, CancellationToken ct);
    Task UpdateAsync(InstagramMediaPost entity, CancellationToken ct);
    Task<DashboardAggregate> GetDashboardStatsAsync(Guid hashtagConfigId, CancellationToken ct);
}

public record DashboardAggregate(int Total, int Approved, int Pending, int Rejected);
