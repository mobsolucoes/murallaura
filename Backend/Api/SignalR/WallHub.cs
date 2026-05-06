using Microsoft.AspNetCore.SignalR;

namespace HashtagWall.Api.SignalR;

public sealed class WallHub : Hub
{
    public static string GroupForHashtag(string normalizedHashtag) => $"wall:{normalizedHashtag.ToLowerInvariant()}";

    public Task Subscribe(string normalizedHashtag)
    {
        var tag = normalizedHashtag.TrimStart('#').ToLowerInvariant();
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupForHashtag(tag));
    }

    public Task Unsubscribe(string normalizedHashtag)
    {
        var tag = normalizedHashtag.TrimStart('#').ToLowerInvariant();
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupForHashtag(tag));
    }
}
