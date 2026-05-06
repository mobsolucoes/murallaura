using HashtagWall.Domain.Entities;

namespace HashtagWall.Application.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct);
    Task EnsureAdminAsync(string username, string passwordHash, CancellationToken ct);
}
