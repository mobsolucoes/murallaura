using HashtagWall.Application.DTOs;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HashtagWall.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/hashtags")]
public sealed class HashtagAdminController : ControllerBase
{
    private readonly IHashtagConfigurationRepository _hashtags;
    private readonly IWallConfigurationRepository _walls;
    private readonly IInstagramMediaRepository _media;

    public HashtagAdminController(
        IHashtagConfigurationRepository hashtags,
        IWallConfigurationRepository walls,
        IInstagramMediaRepository media)
    {
        _hashtags = hashtags;
        _walls = walls;
        _media = media;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HashtagConfigurationDto>>> List(CancellationToken ct)
    {
        var list = await _hashtags.GetAllAsync(ct);
        return Ok(list.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HashtagConfigurationDto>> Get(Guid id, CancellationToken ct)
    {
        var h = await _hashtags.GetByIdAsync(id, ct);
        return h is null ? NotFound() : Ok(Map(h));
    }

    [HttpPost]
    public async Task<ActionResult<HashtagConfigurationDto>> Create([FromBody] UpsertHashtagConfigurationRequest req,
        CancellationToken ct)
    {
        var normalized = HashtagNormalizer.Normalize(req.Hashtag);
        if (string.IsNullOrWhiteSpace(normalized))
            return BadRequest(new { error = "invalid_hashtag" });

        var existing = await _hashtags.GetByNormalizedHashtagAsync(normalized, ct);
        if (existing is not null)
            return Conflict(new { error = "hashtag_exists" });

        if (string.IsNullOrWhiteSpace(req.MetaAccessToken))
            return BadRequest(new { error = "meta_token_required_on_create" });

        var now = DateTimeOffset.UtcNow;
        var cfg = new HashtagConfiguration
        {
            Id = Guid.NewGuid(),
            NormalizedHashtag = normalized,
            InstagramBusinessAccountId = req.InstagramBusinessAccountId.Trim(),
            MetaAccessToken = req.MetaAccessToken.Trim(),
            PollIntervalMinutes = Math.Clamp(req.PollIntervalMinutes, 1, 24 * 60),
            IsMonitoringEnabled = req.IsMonitoringEnabled,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _hashtags.AddAsync(cfg, ct);

        await _walls.UpsertAsync(new WallConfiguration
        {
            Id = Guid.NewGuid(),
            HashtagConfigurationId = cfg.Id,
            Title = $"#{normalized}",
            UpdatedAt = now
        }, ct);

        var created = await _hashtags.GetByIdAsync(cfg.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = cfg.Id }, Map(created!));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HashtagConfigurationDto>> Update(Guid id,
        [FromBody] UpsertHashtagConfigurationRequest req, CancellationToken ct)
    {
        var cfg = await _hashtags.GetByIdAsync(id, ct);
        if (cfg is null)
            return NotFound();

        var normalized = HashtagNormalizer.Normalize(req.Hashtag);
        if (string.IsNullOrWhiteSpace(normalized))
            return BadRequest(new { error = "invalid_hashtag" });

        var other = await _hashtags.GetByNormalizedHashtagAsync(normalized, ct);
        if (other is not null && other.Id != id)
            return Conflict(new { error = "hashtag_exists" });

        cfg.NormalizedHashtag = normalized;
        cfg.InstagramBusinessAccountId = req.InstagramBusinessAccountId.Trim();
        if (!string.IsNullOrWhiteSpace(req.MetaAccessToken))
            cfg.MetaAccessToken = req.MetaAccessToken!.Trim();

        cfg.PollIntervalMinutes = Math.Clamp(req.PollIntervalMinutes, 1, 24 * 60);
        cfg.IsMonitoringEnabled = req.IsMonitoringEnabled;
        cfg.UpdatedAt = DateTimeOffset.UtcNow;

        await _hashtags.UpdateAsync(cfg, ct);
        var updated = await _hashtags.GetByIdAsync(id, ct);
        return Ok(Map(updated!));
    }

    [HttpGet("{id:guid}/dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> Dashboard(Guid id, CancellationToken ct)
    {
        if (await _hashtags.GetByIdAsync(id, ct) is null)
            return NotFound();

        var stats = await _media.GetDashboardStatsAsync(id, ct);
        return Ok(new DashboardStatsDto(stats.Total, stats.Approved, stats.Pending, stats.Rejected));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _hashtags.DeleteAsync(id, ct);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private static HashtagConfigurationDto Map(HashtagConfiguration h) =>
        new(
            h.Id,
            h.NormalizedHashtag,
            h.InstagramBusinessAccountId,
            !string.IsNullOrWhiteSpace(h.MetaAccessToken),
            h.PollIntervalMinutes,
            h.IsMonitoringEnabled,
            h.CreatedAt,
            h.UpdatedAt);
}
