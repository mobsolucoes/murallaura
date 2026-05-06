using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AppDbContext _db;

    public AdminUserRepository(AppDbContext db) => _db = db;

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct) =>
        await _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task EnsureAdminAsync(string username, string passwordHash, CancellationToken ct)
    {
        var exists = await _db.AdminUsers.AnyAsync(u => u.Username == username, ct);
        if (exists)
            return;

        _db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
