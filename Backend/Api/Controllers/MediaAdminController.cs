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

    private static InstagramMediaPostDto Map(InstagramMediaPost m) =>
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
