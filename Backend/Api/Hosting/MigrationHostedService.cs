using HashtagWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Api.Hosting;

public sealed class MigrationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MigrationHostedService> _logger;

    public MigrationHostedService(IServiceScopeFactory scopeFactory, ILogger<MigrationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "HashtagConfigurations"
            ADD COLUMN IF NOT EXISTS "AutoApprovePosts" boolean NOT NULL DEFAULT FALSE;
            """,
            cancellationToken);
        _logger.LogInformation("Database migrations applied.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
