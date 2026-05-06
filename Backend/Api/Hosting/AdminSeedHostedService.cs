using HashtagWall.Application.Interfaces;

namespace HashtagWall.Api.Hosting;

/// <summary>Creates default admin user if missing (password from env SEED_ADMIN_PASSWORD / configuration).</summary>
public sealed class AdminSeedHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeedHostedService> _logger;

    public AdminSeedHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AdminSeedHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var admins = scope.ServiceProvider.GetRequiredService<IAdminUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var username =
            Environment.GetEnvironmentVariable("SEED_ADMIN_USERNAME")
            ?? _configuration["SeedAdmin:Username"]
            ?? "admin";

        var password =
            Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD")
            ?? _configuration["SeedAdmin:Password"]
            ?? "ChangeMe123!";

        var hash = hasher.Hash(password);
        await admins.EnsureAdminAsync(username, hash, cancellationToken);

        _logger.LogInformation("Admin seed ensured for user '{Username}'.", username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
