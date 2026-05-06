using HashtagWall.Application.Interfaces;
using HashtagWall.Infrastructure.Data;
using HashtagWall.Infrastructure.Instagram;
using HashtagWall.Infrastructure.Options;
using HashtagWall.Infrastructure.Repositories;
using HashtagWall.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HashtagWall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var cs = configuration.GetConnectionString("DefaultConnection")
                 ?? configuration["ConnectionStrings:DefaultConnection"];

        services.AddDbContext<AppDbContext>(options =>
        {
            if (!string.IsNullOrEmpty(cs))
                options.UseNpgsql(cs);
        });

        services.AddHttpClient<IInstagramGraphClient, InstagramGraphClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(100);
        });

        services.AddScoped<IHashtagConfigurationRepository, HashtagConfigurationRepository>();
        services.AddScoped<IInstagramMediaRepository, InstagramMediaRepository>();
        services.AddScoped<IWallConfigurationRepository, WallConfigurationRepository>();
        services.AddScoped<IIntegrationLogRepository, IntegrationLogRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IInstagramSyncService, InstagramSyncService>();

        return services;
    }
}
