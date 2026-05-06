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
    private static readonly string[] CheckpointSignals =
    [
        "/challenge/",
        "/accounts/suspended/",
        "two_factor",
        "checkpoint"
    ];

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
        {
            _logger.LogDebug(
                "Instagram Web RPA skipped for #{Tag}. Enabled={Enabled}, UsernameConfigured={HasUsername}, PasswordConfigured={HasPassword}.",
                cfg.NormalizedHashtag,
                opt.Enabled,
                !string.IsNullOrWhiteSpace(opt.Username),
                !string.IsNullOrWhiteSpace(opt.Password));
            return 0;
        }

        try
        {
            _logger.LogInformation(
                "Instagram Web RPA sync started for #{Tag}. MaxPosts={MaxPosts}, RetryAttempts={RetryAttempts}, Headless={Headless}.",
                cfg.NormalizedHashtag,
                opt.MaxPostsPerRun,
                opt.MaxRetryAttempts,
                opt.Headless);
            var posts = await ExecuteWithRetryAsync(
                cfg.NormalizedHashtag,
                opt,
                ct);
            _logger.LogInformation("Scraping finished for #{Tag}. Candidate posts found: {Count}.", cfg.NormalizedHashtag, posts.Count);
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

            _logger.LogInformation("Instagram Web RPA sync finished for #{Tag}. Inserted={Inserted}.", cfg.NormalizedHashtag, inserted);
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

    private async Task<IReadOnlyList<ScrapedPost>> ExecuteWithRetryAsync(
        string hashtag,
        InstagramWebRpaOptions opt,
        CancellationToken ct)
    {
        Exception? lastError = null;
        var attempts = Math.Max(opt.MaxRetryAttempts, 1);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                _logger.LogInformation("RPA attempt {Attempt}/{Total} for #{Tag}.", attempt, attempts, hashtag);
                return await ScrapeHashtagPostsAsync(hashtag, opt, ct);
            }
            catch (Exception ex) when (attempt < attempts)
            {
                lastError = ex;
                var backoff = TimeSpan.FromSeconds(Math.Max(opt.InitialBackoffSeconds, 1) * attempt);
                _logger.LogWarning(ex,
                    "Instagram RPA attempt {Attempt}/{Total} failed for #{Tag}. Retrying in {Delay}s.",
                    attempt, attempts, hashtag, backoff.TotalSeconds);
                await Task.Delay(backoff, ct);
            }
            catch (Exception ex)
            {
                lastError = ex;
                break;
            }
        }

        throw new InvalidOperationException($"Instagram RPA failed after {attempts} attempt(s).", lastError);
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

        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1366, Height = 768 }
        };
        if (!string.IsNullOrWhiteSpace(opt.SessionStatePath) && File.Exists(opt.SessionStatePath))
            contextOptions.StorageStatePath = opt.SessionStatePath;

        var context = await browser.NewContextAsync(contextOptions);
        _logger.LogDebug("Browser context created for #{Tag}. SessionStatePath={SessionStatePath}.", hashtag, opt.SessionStatePath);

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(opt.NavigationTimeoutMs);

        var isAuthenticated = await EnsureAuthenticatedAsync(page, context, opt, timeoutCts.Token);
        if (!isAuthenticated)
        {
            throw new InvalidOperationException("Instagram login did not complete. Validate credentials or checkpoint challenge.");
        }

        var hashtagUrl = $"https://www.instagram.com/explore/tags/{hashtag.Trim().TrimStart('#')}/";
        await page.GotoAsync(hashtagUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        EnsureNoCheckpointOrTwoFactor(page.Url, opt);
        var hasPosts = await WaitForPostsOrEmptyStateAsync(page, opt.NavigationTimeoutMs);
        if (!hasPosts)
        {
            _logger.LogWarning("No posts found (or page not exposing post links) for #{Tag}. Returning empty batch.", hashtag);
            await context.CloseAsync();
            return Array.Empty<ScrapedPost>();
        }
        _logger.LogInformation("Hashtag page loaded for #{Tag}.", hashtag);

        var links = await page.EvaluateAsync<string[]>(
            "Array.from(document.querySelectorAll(\"a[href*='/p/'],a[href*='/reel/']\")).map(a => a.href)");

        var uniqueLinks = links.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(Math.Max(opt.MaxPostsPerRun, 1)).ToList();
        var posts = new List<ScrapedPost>(uniqueLinks.Count);
        _logger.LogInformation("Collected {Count} unique post links for #{Tag}.", uniqueLinks.Count, hashtag);

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

    private async Task<bool> EnsureAuthenticatedAsync(
        IPage page,
        IBrowserContext context,
        InstagramWebRpaOptions opt,
        CancellationToken ct)
    {
        await page.GotoAsync("https://www.instagram.com/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        EnsureNoCheckpointOrTwoFactor(page.Url, opt);
        if (!NeedsLogin(page.Url))
        {
            _logger.LogInformation("Instagram session already authenticated.");
            return true;
        }

        _logger.LogInformation("Instagram login required. Executing credential login.");
        await page.GotoAsync("https://www.instagram.com/accounts/login/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        EnsureNoCheckpointOrTwoFactor(page.Url, opt);
        await page.Locator("input[name='username']").FillAsync(opt.Username);
        await page.Locator("input[name='password']").FillAsync(opt.Password);
        await page.Locator("button[type='submit']").ClickAsync();
        await page.WaitForTimeoutAsync(Math.Max(opt.PauseAfterLoginMs, 0));

        await page.GotoAsync("https://www.instagram.com/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        EnsureNoCheckpointOrTwoFactor(page.Url, opt);

        var loggedIn = !NeedsLogin(page.Url);
        if (loggedIn && !string.IsNullOrWhiteSpace(opt.SessionStatePath))
        {
            var directory = Path.GetDirectoryName(opt.SessionStatePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await context.StorageStateAsync(new BrowserContextStorageStateOptions
            {
                Path = opt.SessionStatePath
            });
            _logger.LogInformation("Instagram session state saved to {Path}.", opt.SessionStatePath);
        }

        _logger.LogInformation("Instagram login result: {LoggedIn}.", loggedIn);
        return loggedIn;
    }

    private static bool NeedsLogin(string currentUrl) =>
        currentUrl.Contains("/accounts/login", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> WaitForPostsOrEmptyStateAsync(IPage page, int timeoutMs)
    {
        var selector = "a[href*='/p/'],a[href*='/reel/']";
        var deadline = DateTime.UtcNow.AddMilliseconds(Math.Max(timeoutMs, 1000));

        while (DateTime.UtcNow < deadline)
        {
            var links = await page.QuerySelectorAllAsync(selector);
            if (links.Count > 0)
                return true;

            var bodyText = (await page.Locator("body").InnerTextAsync()).ToLowerInvariant();
            if (bodyText.Contains("nenhuma publicação") ||
                bodyText.Contains("no posts yet") ||
                bodyText.Contains("sem publicações"))
            {
                return false;
            }

            await page.Mouse.WheelAsync(0, 1200);
            await page.WaitForTimeoutAsync(1000);
        }

        return false;
    }

    private static void EnsureNoCheckpointOrTwoFactor(string currentUrl, InstagramWebRpaOptions opt)
    {
        if (!opt.FailOnCheckpointOrTwoFactor)
            return;

        var lower = currentUrl.ToLowerInvariant();
        if (CheckpointSignals.Any(signal => lower.Contains(signal)))
            throw new InvalidOperationException("Instagram blocked automation with checkpoint/two-factor flow. Manual login approval is required.");
    }

    private static string? ExtractMediaId(string permalink)
    {
        var match = PostCodeRegex.Match(permalink);
        return match.Success ? match.Groups[1].Value : null;
    }

    private sealed record ScrapedPost(string MediaId, string Permalink, string? MediaUrl, string? Caption);
}
