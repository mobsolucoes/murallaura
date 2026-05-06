using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Infrastructure.Repositories;

public class WallConfigurationRepository : IWallConfigurationRepository
{
    private readonly AppDbContext _db;

    public WallConfigurationRepository(AppDbContext db) => _db = db;

    public async Task<WallConfiguration?> GetByHashtagConfigIdAsync(Guid hashtagConfigurationId, CancellationToken ct) =>
        await _db.WallConfigurations.AsNoTracking()
            .FirstOrDefaultAsync(w => w.HashtagConfigurationId == hashtagConfigurationId, ct);

    public async Task<WallConfiguration> UpsertAsync(WallConfiguration entity, CancellationToken ct)
    {
        var existing = await _db.WallConfigurations.FirstOrDefaultAsync(w => w.HashtagConfigurationId == entity.HashtagConfigurationId, ct);
        if (existing is null)
        {
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            _db.WallConfigurations.Add(entity);
        }
        else
        {
            existing.Title = entity.Title;
            existing.LogoUrl = entity.LogoUrl;
            existing.PrimaryColor = entity.PrimaryColor;
            existing.SecondaryColor = entity.SecondaryColor;
            existing.AccentColor = entity.AccentColor;
            existing.Theme = entity.Theme;
            existing.DisplayDurationSeconds = entity.DisplayDurationSeconds;
            existing.ShowCaption = entity.ShowCaption;
            existing.ShowQrCode = entity.ShowQrCode;
            existing.UpdatedAt = entity.UpdatedAt;
        }

        await _db.SaveChangesAsync(ct);

        var saved = await _db.WallConfigurations.AsNoTracking()
            .FirstAsync(w => w.HashtagConfigurationId == entity.HashtagConfigurationId, ct);
        return saved;
    }
}
