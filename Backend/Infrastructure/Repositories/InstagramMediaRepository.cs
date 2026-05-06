using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Domain.Enums;
using HashtagWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Infrastructure.Repositories;

public class InstagramMediaRepository : IInstagramMediaRepository
{
    private readonly AppDbContext _db;

    public InstagramMediaRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InstagramMediaPost>> ListByHashtagAsync(Guid hashtagConfigId, CancellationToken ct) =>
        await _db.InstagramMediaPosts.AsNoTracking()
            .Where(m => m.HashtagConfigurationId == hashtagConfigId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InstagramMediaPost>> ListApprovedByHashtagAsync(Guid hashtagConfigId, CancellationToken ct) =>
        await _db.InstagramMediaPosts.AsNoTracking()
            .Where(m => m.HashtagConfigurationId == hashtagConfigId && m.Status == MediaPostStatus.Approved)
            // UpdatedAt is touched on approval/re-approval, so this reflects approval recency.
            .OrderByDescending(m => m.UpdatedAt)
            .ThenByDescending(m => m.Timestamp)
            .ToListAsync(ct);

    public async Task<InstagramMediaPost?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.InstagramMediaPosts.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<bool> ExistsByInstagramMediaIdAsync(Guid hashtagConfigId, string instagramMediaId, CancellationToken ct) =>
        await _db.InstagramMediaPosts.AnyAsync(m => m.HashtagConfigurationId == hashtagConfigId && m.InstagramMediaId == instagramMediaId, ct);

    public async Task<InstagramMediaPost> AddAsync(InstagramMediaPost entity, CancellationToken ct)
    {
        _db.InstagramMediaPosts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(InstagramMediaPost entity, CancellationToken ct)
    {
        _db.InstagramMediaPosts.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DashboardAggregate> GetDashboardStatsAsync(Guid hashtagConfigId, CancellationToken ct)
    {
        var query = _db.InstagramMediaPosts.Where(m => m.HashtagConfigurationId == hashtagConfigId);
        var total = await query.CountAsync(ct);
        var approved = await query.CountAsync(m => m.Status == MediaPostStatus.Approved, ct);
        var pending = await query.CountAsync(m => m.Status == MediaPostStatus.Pending, ct);
        var rejected = await query.CountAsync(m => m.Status == MediaPostStatus.Rejected, ct);
        return new DashboardAggregate(total, approved, pending, rejected);
    }
}
