namespace HashtagWall.Application.Interfaces;

/// <summary>Notify SignalR groups when approved media changes.</summary>
public interface IWallRealtimeNotifier
{
    Task NotifyApprovedMediaChangedAsync(string normalizedHashtag, CancellationToken ct);
}
