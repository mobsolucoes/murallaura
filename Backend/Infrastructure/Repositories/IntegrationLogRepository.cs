using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Infrastructure.Repositories;

public class IntegrationLogRepository : IIntegrationLogRepository
{
    private const int MessageMaxLength = 1024;
    private const int DetailsMaxLength = 16384;

    private readonly AppDbContext _db;

    public IntegrationLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(IntegrationLog log, CancellationToken ct)
    {
        log.Message = TruncateRequired(log.Message, MessageMaxLength);
        log.Details = TruncateOptional(log.Details, DetailsMaxLength);

        _db.IntegrationLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IntegrationLog>> ListRecentAsync(Guid? hashtagConfigurationId, int take, CancellationToken ct)
    {
        var q = _db.IntegrationLogs.AsNoTracking().OrderByDescending(l => l.CreatedAt);
        if (hashtagConfigurationId is { } hid)
            return await q.Where(l => l.HashtagConfigurationId == hid).Take(take).ToListAsync(ct);
        return await q.Take(take).ToListAsync(ct);
    }

    private static string TruncateRequired(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }

    private static string? TruncateOptional(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
