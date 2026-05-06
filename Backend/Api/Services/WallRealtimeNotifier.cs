using HashtagWall.Application.Interfaces;
using HashtagWall.Api.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace HashtagWall.Api.Services;

public sealed class WallRealtimeNotifier : IWallRealtimeNotifier
{
    private readonly IHubContext<WallHub> _hub;

    public WallRealtimeNotifier(IHubContext<WallHub> hub) => _hub = hub;

    public async Task NotifyApprovedMediaChangedAsync(string normalizedHashtag, CancellationToken ct)
    {
        var tag = normalizedHashtag.TrimStart('#').ToLowerInvariant();
        var group = WallHub.GroupForHashtag(tag);
        await _hub.Clients.Group(group).SendAsync("ApprovedPostsUpdated", cancellationToken: ct);
    }
}
