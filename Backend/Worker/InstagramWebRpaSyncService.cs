using System.Text.RegularExpressions;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace HashtagWall.Worker;

public sealed class InstagramWebRpaSyncService
{
    private static readonly Regex PostCodeRegex = new("/(?:p|reel)/([^/?#]+)/?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IHashtagConfigurationRepository _hashtagRepo;
    private readonly IInstagramMediaRepository _mediaRepo;
    private readonly IIntegrationLogRepository _logRepo;
    private readonly IOptionsMonitor<InstagramWebRpaOptions> _options;
    private readonly ILogger<InstagramWebRpaSyncService> _logger;

    public InstagramWebRpaSyncService(
        IHashtagConfigurationRepository hashtagRepo,
        IInstagramMediaRepository mediaRepo,
        IIntegrationLogRepository logRepo,
        IOptionsMonitor<InstagramWebRpaOptions> options,
        ILogger<InstagramWebRpaSyncService> logger)
    {
        _hashtagRepo = hashtagRepo;
        _mediaRepo = mediaRepo;
        _logRepo = logRepo;
        _options = options;
        _logger = logger;
    }

    public async Task<int> SyncHashtagAsync(Guid hashtagConfigurationId, CancellationToken ct)
    {
        var cfg = await _hashtagRepo.GetByIdAsync(hashtagConfigurationId, ct);
        if (cfg is null || !cfg.IsMonitoringEnabled)
            return 0;

        var opt = _options.CurrentValue;
        if (!opt.Enabled || string.IsNullOrWhiteSpace(opt.Username) || string.IsNullOrWhiteSpace(opt.Password))
            return 0;

        try
        {
            var posts = await ScrapeHashtagPostsAsync(cfg.NormalizedHashtag, opt, ct);
            var inserted = 0;

            foreach (var post in posts)
            {
                if (await _mediaRepo.ExistsByInstagramMediaIdAsync(cfg.Id, post.MediaId, ct))
                    continue;

                var entity = new InstagramMediaPost
                {
                    Id = Guid.NewGuid(),
                    HashtagConfigurationId = cfg.Id,
                    InstagramMediaId = post.MediaId,
                    Hashtag = cfg.NormalizedHashtag,
                    Caption = post.Caption,
                    MediaUrl = post.MediaUrl,
                    ThumbnailUrl = post.MediaUrl,
                    Permalink = post.Permalink,
                    MediaType = "IMAGE",
                    Timestamp = DateTimeOffset.UtcNow,
                    Status = MediaPostStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                await _mediaRepo.AddAsync(entity, ct);
                inserted++;
            }

            cfg.LastSyncedAt = DateTimeOffset.UtcNow;
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            await _hashtagRepo.UpdateAsync(cfg, ct);

            await _logRepo.AddAsync(new IntegrationLog
            {
                Id = Guid.NewGuid(),
                HashtagConfigurationId = cfg.Id,
                Level = IntegrationLogLevel.Information,
                Message = $"Instagram Web RPA sync OK for #{cfg.NormalizedHashtag}: {inserted} new post(s).",
                CreatedAt = DateTimeOffset.UtcNow
            }, ct);

            return inserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Instagram Web RPA sync failed for #{Tag}", cfg.NormalizedHashtag);
            await _logRepo.AddAsync(new IntegrationLog
            {
                Id = Guid.NewGuid(),
                HashtagConfigurationId = cfg.Id,
                Level = IntegrationLogLevel.Error,
                Message = $"Instagram Web RPA sync error: {ex.Message}",
                Details = ex.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            }, ct);

            cfg.LastSyncedAt = DateTimeOffset.UtcNow;
            cfg.UpdatedAt = DateTimeOffset.UtcNow;
            await _hashtagRepo.UpdateAsync(cfg, ct);
            return 0;
        }
    }

    private async Task<IReadOnlyList<ScrapedPost>> ScrapeHashtagPostsAsync(string hashtag, InstagramWebRpaOptions opt, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = opt.Headless
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1366, Height = 768 }
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(opt.NavigationTimeoutMs);

        await page.GotoAsync("https://www.instagram.com/accounts/login/");
        await page.Locator("input[name='username']").FillAsync(opt.Username);
        await page.Locator("input[name='password']").FillAsync(opt.Password);
        await page.Locator("button[type='submit']").ClickAsync();

        await page.WaitForURLAsync("**/instagram.com/**", new PageWaitForURLOptions
        {
            Timeout = opt.NavigationTimeoutMs
        });

        var hashtagUrl = $"https://www.instagram.com/explore/tags/{hashtag.Trim().TrimStart('#')}/";
        await page.GotoAsync(hashtagUrl);
        await page.WaitForSelectorAsync("a[href*='/p/'],a[href*='/reel/']");

        var links = await page.EvaluateAsync<string[]>(
            "Array.from(document.querySelectorAll(\"a[href*='/p/'],a[href*='/reel/']\")).map(a => a.href)");

        var uniqueLinks = links.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(Math.Max(opt.MaxPostsPerRun, 1)).ToList();
        var posts = new List<ScrapedPost>(uniqueLinks.Count);

        foreach (var link in uniqueLinks)
        {
            var mediaId = ExtractMediaId(link);
            if (string.IsNullOrWhiteSpace(mediaId))
                continue;

            await page.GotoAsync(link, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            var mediaUrl = await page.Locator("meta[property='og:image']").GetAttributeAsync("content");
            var caption = await page.Locator("meta[property='og:title']").GetAttributeAsync("content");

            posts.Add(new ScrapedPost(mediaId, link, mediaUrl, caption));
        }

        await context.CloseAsync();
        return posts;
    }

    private static string? ExtractMediaId(string permalink)
    {
        var match = PostCodeRegex.Match(permalink);
        return match.Success ? match.Groups[1].Value : null;
    }

    private sealed record ScrapedPost(string MediaId, string Permalink, string? MediaUrl, string? Caption);
}
