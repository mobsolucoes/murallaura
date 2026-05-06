using HashtagWall.Application.DTOs;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using HashtagWall.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HashtagWall.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin")]
public sealed class MediaAdminController : ControllerBase
{
    private readonly IInstagramMediaRepository _media;
    private readonly IHashtagConfigurationRepository _hashtags;
    private readonly IWallRealtimeNotifier _notifier;

    public MediaAdminController(
        IInstagramMediaRepository media,
        IHashtagConfigurationRepository hashtags,
        IWallRealtimeNotifier notifier)
    {
        _media = media;
        _hashtags = hashtags;
        _notifier = notifier;
    }

    [HttpGet("hashtags/{hashtagConfigId:guid}/media")]
    public async Task<ActionResult<IReadOnlyList<InstagramMediaPostDto>>> List(Guid hashtagConfigId,
        CancellationToken ct)
    {
        if (await _hashtags.GetByIdAsync(hashtagConfigId, ct) is null)
            return NotFound();

        var items = await _media.ListByHashtagAsync(hashtagConfigId, ct);
        return Ok(items.Select(Map).ToList());
    }

    [HttpPost("media/{postId:guid}/approve")]
    public async Task<ActionResult> Approve(Guid postId, CancellationToken ct)
    {
        var post = await _media.GetByIdAsync(postId, ct);
        if (post is null)
            return NotFound();

        post.Status = MediaPostStatus.Approved;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _media.UpdateAsync(post, ct);

        await _notifier.NotifyApprovedMediaChangedAsync(post.Hashtag, ct);
        return NoContent();
    }

    [HttpPost("media/{postId:guid}/reject")]
    public async Task<ActionResult> Reject(Guid postId, CancellationToken ct)
    {
        var post = await _media.GetByIdAsync(postId, ct);
        if (post is null)
            return NotFound();

        post.Status = MediaPostStatus.Rejected;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _media.UpdateAsync(post, ct);
        return NoContent();
    }

    [HttpDelete("media/{postId:guid}")]
    public async Task<ActionResult> Delete(Guid postId, CancellationToken ct)
    {
        var post = await _media.GetByIdAsync(postId, ct);
        if (post is null)
            return NotFound();

        var deleted = await _media.DeleteAsync(postId, ct);
        if (!deleted)
            return NotFound();

        if (post.Status == MediaPostStatus.Approved)
            await _notifier.NotifyApprovedMediaChangedAsync(post.Hashtag, ct);

        return NoContent();
    }

    [HttpPost("media/bulk-delete")]
    public async Task<ActionResult<object>> BulkDelete([FromBody] BulkDeleteMediaRequest request, CancellationToken ct)
    {
        var ids = request.PostIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray() ?? Array.Empty<Guid>();

        if (request.HashtagConfigId == Guid.Empty)
            return BadRequest(new { error = "hashtag_config_id_required" });

        if (ids.Length == 0)
            return BadRequest(new { error = "post_ids_required" });

        var found = await _media.ListByIdsAsync(ids, ct);
        var invalid = found.Any(p => p.HashtagConfigurationId != request.HashtagConfigId);
        if (invalid)
            return BadRequest(new { error = "invalid_post_scope" });

        var deleted = await _media.DeleteManyAsync(ids, ct);
        var approvedTags = found
            .Where(p => p.Status == MediaPostStatus.Approved)
            .Select(p => p.Hashtag)
            .Distinct()
            .ToArray();

        foreach (var tag in approvedTags)
            await _notifier.NotifyApprovedMediaChangedAsync(tag, ct);

        return Ok(new { deleted });
    }

    private InstagramMediaPostDto Map(InstagramMediaPost m) =>
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

    private string? ToClientMediaUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return rawUrl;

        if (rawUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return $"{Request.Scheme}://{Request.Host}{rawUrl}";

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return rawUrl;

        if (uri.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return $"{Request.Scheme}://{Request.Host}{uri.AbsolutePath}";

        return rawUrl;
    }

    public sealed class BulkDeleteMediaRequest
    {
        public Guid HashtagConfigId { get; set; }
        public IReadOnlyList<Guid>? PostIds { get; set; }
    }
}
