using HashtagWall.Application.DTOs;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HashtagWall.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/hashtags/{hashtagConfigId:guid}/wall")]
public sealed class WallAdminController : ControllerBase
{
    private readonly IHashtagConfigurationRepository _hashtags;
    private readonly IWallConfigurationRepository _walls;

    public WallAdminController(IHashtagConfigurationRepository hashtags, IWallConfigurationRepository walls)
    {
        _hashtags = hashtags;
        _walls = walls;
    }

    [HttpGet]
    public async Task<ActionResult<WallConfigurationDto>> Get(Guid hashtagConfigId, CancellationToken ct)
    {
        if (await _hashtags.GetByIdAsync(hashtagConfigId, ct) is null)
            return NotFound();

        var w = await _walls.GetByHashtagConfigIdAsync(hashtagConfigId, ct);
        if (w is null)
            return NotFound();

        return Ok(Map(w));
    }

    [HttpPut]
    public async Task<ActionResult<WallConfigurationDto>> Upsert(Guid hashtagConfigId,
        [FromBody] UpdateWallConfigurationRequest req, CancellationToken ct)
    {
        if (await _hashtags.GetByIdAsync(hashtagConfigId, ct) is null)
            return NotFound();

        var now = DateTimeOffset.UtcNow;
        var entity = new WallConfiguration
        {
            HashtagConfigurationId = hashtagConfigId,
            Title = req.Title.Trim(),
            LogoUrl = string.IsNullOrWhiteSpace(req.LogoUrl) ? null : req.LogoUrl.Trim(),
            PrimaryColor = req.PrimaryColor.Trim(),
            SecondaryColor = req.SecondaryColor.Trim(),
            AccentColor = req.AccentColor.Trim(),
            Theme = req.Theme.Trim(),
            DisplayDurationSeconds = Math.Clamp(req.DisplayDurationSeconds, 5, 3600),
            ShowCaption = req.ShowCaption,
            ShowQrCode = req.ShowQrCode,
            UpdatedAt = now
        };

        var saved = await _walls.UpsertAsync(entity, ct);
        return Ok(Map(saved));
    }

    private static WallConfigurationDto Map(WallConfiguration w) =>
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
}
