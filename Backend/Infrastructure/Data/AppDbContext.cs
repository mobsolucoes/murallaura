using HashtagWall.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HashtagWall.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<HashtagConfiguration> HashtagConfigurations => Set<HashtagConfiguration>();
    public DbSet<WallConfiguration> WallConfigurations => Set<WallConfiguration>();
    public DbSet<InstagramMediaPost> InstagramMediaPosts => Set<InstagramMediaPost>();
    public DbSet<IntegrationLog> IntegrationLogs => Set<IntegrationLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HashtagConfiguration>(e =>
        {
            e.HasIndex(x => x.NormalizedHashtag).IsUnique();
            e.Property(x => x.NormalizedHashtag).HasMaxLength(256);
            e.Property(x => x.InstagramBusinessAccountId).HasMaxLength(64);
            e.Property(x => x.MetaAccessToken).HasMaxLength(4096);
        });

        modelBuilder.Entity<WallConfiguration>(e =>
        {
            e.HasIndex(x => x.HashtagConfigurationId).IsUnique();
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.PrimaryColor).HasMaxLength(32);
            e.Property(x => x.SecondaryColor).HasMaxLength(32);
            e.Property(x => x.AccentColor).HasMaxLength(32);
            e.Property(x => x.Theme).HasMaxLength(32);
            e.Property(x => x.LogoUrl).HasMaxLength(2048);
        });

        modelBuilder.Entity<InstagramMediaPost>(e =>
        {
            e.HasIndex(x => new { x.HashtagConfigurationId, x.InstagramMediaId }).IsUnique();
            e.Property(x => x.InstagramMediaId).HasMaxLength(128);
            e.Property(x => x.Hashtag).HasMaxLength(256);
            e.Property(x => x.Permalink).HasMaxLength(2048);
            e.Property(x => x.MediaType).HasMaxLength(64);
            e.Property(x => x.Caption).HasMaxLength(8192);
            e.Property(x => x.MediaUrl).HasMaxLength(4096);
            e.Property(x => x.ThumbnailUrl).HasMaxLength(4096);
        });

        modelBuilder.Entity<IntegrationLog>(e =>
        {
            e.Property(x => x.Message).HasMaxLength(1024);
            e.Property(x => x.Details).HasMaxLength(16384);
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(256);
        });
    }
}
