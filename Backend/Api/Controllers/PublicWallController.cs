using HashtagWall.Application.DTOs;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Domain.Enums;
using HashtagWall.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HashtagWall.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/public/wall")]
public sealed class PublicWallController : ControllerBase
{
    private readonly IHashtagConfigurationRepository _hashtags;
    private readonly IWallConfigurationRepository _walls;
    private readonly IInstagramMediaRepository _media;
    private readonly IWallRealtimeNotifier _notifier;
    private readonly IWebHostEnvironment _env;

    public PublicWallController(
        IHashtagConfigurationRepository hashtags,
        IWallConfigurationRepository walls,
        IInstagramMediaRepository media,
        IWallRealtimeNotifier notifier,
        IWebHostEnvironment env)
    {
        _hashtags = hashtags;
        _walls = walls;
        _media = media;
        _notifier = notifier;
        _env = env;
    }

    /// <summary>Example path: /api/public/wall/myevent/config</summary>
    [HttpGet("{hashtag}/config")]
    public async Task<ActionResult<WallConfigurationDto>> Config(string hashtag, CancellationToken ct)
    {
        var normalized = HashtagNormalizer.Normalize(hashtag);
        var cfg = await _hashtags.GetByNormalizedHashtagAsync(normalized, ct);
        if (cfg is null)
            return NotFound();

        var w = await _walls.GetByHashtagConfigIdAsync(cfg.Id, ct);
        if (w is null)
            return NotFound();

        return Ok(MapWall(w));
    }

    /// <summary>Approved posts oldest-first for carousel.</summary>
    [HttpGet("{hashtag}/approved-posts")]
    public async Task<ActionResult<IReadOnlyList<InstagramMediaPostDto>>> ApprovedPosts(string hashtag,
        CancellationToken ct)
    {
        var normalized = HashtagNormalizer.Normalize(hashtag);
        var cfg = await _hashtags.GetByNormalizedHashtagAsync(normalized, ct);
        if (cfg is null)
            return NotFound();

        var items = await _media.ListApprovedByHashtagAsync(cfg.Id, ct);
        return Ok(items.Select(MapPost).ToList());
    }

    [HttpPost("{hashtag}/submit")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> Submit(string hashtag, [FromForm] PublicUploadRequest req, CancellationToken ct)
    {
        if (req.File is null || req.File.Length == 0)
            return BadRequest(new { error = "file_required" });

        var normalized = HashtagNormalizer.Normalize(hashtag);
        var cfg = await _hashtags.GetByNormalizedHashtagAsync(normalized, ct);
        if (cfg is null)
            return NotFound(new { error = "hashtag_not_found" });

        var ext = Path.GetExtension(req.File.FileName).ToLowerInvariant();
        var isImage = ext is ".jpg" or ".jpeg" or ".png" or ".webp";
        var isVideo = ext is ".mp4" or ".mov" or ".webm";
        if (!isImage && !isVideo)
            return BadRequest(new { error = "unsupported_file_type" });

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsRoot, fileName);
        await using (var fs = System.IO.File.Create(filePath))
        {
            await req.File.CopyToAsync(fs, ct);
        }

        var publicUrl = BuildAbsoluteUploadUrl(fileName);
        var now = DateTimeOffset.UtcNow;
        var entity = new InstagramMediaPost
        {
            Id = Guid.NewGuid(),
            HashtagConfigurationId = cfg.Id,
            InstagramMediaId = $"platform:{Guid.NewGuid():N}",
            Hashtag = normalized,
            Caption = req.Caption?.Trim(),
            MediaUrl = publicUrl,
            ThumbnailUrl = isVideo ? null : publicUrl,
            Permalink = publicUrl,
            MediaType = isVideo ? "VIDEO" : "IMAGE",
            Timestamp = now,
            Status = cfg.AutoApprovePosts ? MediaPostStatus.Approved : MediaPostStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _media.AddAsync(entity, ct);
        if (entity.Status == MediaPostStatus.Approved)
            await _notifier.NotifyApprovedMediaChangedAsync(normalized, ct);

        return Ok(new { ok = true, mediaUrl = publicUrl, status = entity.Status.ToString() });
    }

    private static WallConfigurationDto MapWall(WallConfiguration w) =>
        new(
            w.Id,
            w.HashtagConfigurationId,
            w.Title,
            w.LogoUrl,
            w.PrimaryColor,
            w.SecondaryColor,
            w.AccentColor,
            w.Theme,
            w.DisplayDurationSeconds,
            w.ShowCaption,
            w.ShowQrCode);

    private InstagramMediaPostDto MapPost(InstagramMediaPost m) =>
        new(
            m.Id,
            m.InstagramMediaId,
            m.Hashtag,
            m.Caption,
            ToClientMediaUrl(m.MediaUrl),
            ToClientMediaUrl(m.ThumbnailUrl),
            m.Permalink,
            m.MediaType,
            m.Timestamp,
            m.Status,
            m.CreatedAt);

    private string BuildAbsoluteUploadUrl(string fileName) =>
        $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

    private string? ToClientMediaUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return rawUrl;

        if (rawUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return $"{Request.Scheme}://{Request.Host}{rawUrl}";

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return rawUrl;

        // Normalize uploaded files to the current public host/scheme (avoids mixed content and internal hosts).
        if (uri.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return $"{Request.Scheme}://{Request.Host}{uri.AbsolutePath}";

        return rawUrl;
    }

    public sealed class PublicUploadRequest
    {
        public IFormFile? File { get; set; }
        public string? Caption { get; set; }
    }
}
