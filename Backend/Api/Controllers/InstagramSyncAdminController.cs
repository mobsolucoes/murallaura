using HashtagWall.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HashtagWall.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/hashtags/{hashtagConfigId:guid}/sync")]
public sealed class InstagramSyncAdminController : ControllerBase
{
    private readonly IInstagramSyncService _sync;
    private readonly IHashtagConfigurationRepository _hashtags;

    public InstagramSyncAdminController(IInstagramSyncService sync, IHashtagConfigurationRepository hashtags)
    {
        _sync = sync;
        _hashtags = hashtags;
    }

    [HttpPost]
    public async Task<ActionResult<object>> RunSync(Guid hashtagConfigId, CancellationToken ct)
    {
        if (await _hashtags.GetByIdAsync(hashtagConfigId, ct) is null)
            return NotFound();

        var inserted = await _sync.SyncHashtagAsync(hashtagConfigId, ct);
        return Ok(new { inserted });
    }
}
