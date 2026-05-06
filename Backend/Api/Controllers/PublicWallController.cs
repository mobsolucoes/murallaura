using HashtagWall.Application.DTOs;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
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

    public PublicWallController(
        IHashtagConfigurationRepository hashtags,
        IWallConfigurationRepository walls,
        IInstagramMediaRepository media)
    {
        _hashtags = hashtags;
        _walls = walls;
        _media = media;
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

    private static InstagramMediaPostDto MapPost(InstagramMediaPost m) =>
        new(
            m.Id,
            m.InstagramMediaId,
            m.Hashtag,
            m.Caption,
            m.MediaUrl,
            m.ThumbnailUrl,
            m.Permalink,
            m.MediaType,
            m.Timestamp,
            m.Status,
            m.CreatedAt);
}
