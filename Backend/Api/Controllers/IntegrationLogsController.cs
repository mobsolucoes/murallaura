using HashtagWall.Application.DTOs;
using HashtagWall.Application.Interfaces;
using HashtagWall.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HashtagWall.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/admin/integration-logs")]
public sealed class IntegrationLogsController : ControllerBase
{
    private readonly IIntegrationLogRepository _logs;

    public IntegrationLogsController(IIntegrationLogRepository logs) => _logs = logs;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IntegrationLogDto>>> List(
        [FromQuery] Guid? hashtagConfigurationId,
        [FromQuery] int take = 100,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var items = await _logs.ListRecentAsync(hashtagConfigurationId, take, ct);
        return Ok(items.Select(Map).ToList());
    }

    private static IntegrationLogDto Map(IntegrationLog l) =>
        new(l.Id, l.HashtagConfigurationId, l.Level, l.Message, l.Details, l.CreatedAt);
}
