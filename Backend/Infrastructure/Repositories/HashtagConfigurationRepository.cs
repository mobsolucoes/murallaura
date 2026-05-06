using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Infrastructure.Repositories;

public class HashtagConfigurationRepository : IHashtagConfigurationRepository
{
    private readonly AppDbContext _db;

    public HashtagConfigurationRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<HashtagConfiguration>> GetAllAsync(CancellationToken ct) =>
        await _db.HashtagConfigurations.AsNoTracking().OrderBy(h => h.NormalizedHashtag).ToListAsync(ct);

    public async Task<HashtagConfiguration?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.HashtagConfigurations.FirstOrDefaultAsync(h => h.Id == id, ct);

    public async Task<HashtagConfiguration?> GetByNormalizedHashtagAsync(string normalized, CancellationToken ct) =>
        await _db.HashtagConfigurations.FirstOrDefaultAsync(h => h.NormalizedHashtag == normalized, ct);

    public async Task<HashtagConfiguration> AddAsync(HashtagConfiguration entity, CancellationToken ct)
    {
        _db.HashtagConfigurations.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(HashtagConfiguration entity, CancellationToken ct)
    {
        _db.HashtagConfigurations.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
