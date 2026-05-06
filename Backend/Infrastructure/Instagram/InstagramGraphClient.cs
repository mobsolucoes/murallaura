using System.Text.Json;
using HashtagWall.Application.Interfaces;

namespace HashtagWall.Infrastructure.Instagram;

public class InstagramGraphClient : IInstagramGraphClient
{
    private const string ApiVersion = "v21.0";
    private readonly HttpClient _http;

    public InstagramGraphClient(HttpClient http) => _http = http;

    public async Task<string?> ResolveHashtagIdAsync(string accessToken, string instagramUserId, string hashtagWithoutHash, CancellationToken ct)
    {
        var url =
            $"https://graph.facebook.com/{ApiVersion}/ig_hashtag_search?user_id={Uri.EscapeDataString(instagramUserId)}&q={Uri.EscapeDataString(hashtagWithoutHash)}&access_token={Uri.EscapeDataString(accessToken)}";

        await using var stream = await SendAndReadStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (TryGetErrorMessage(root, out var err))
            throw new GraphApiException(err ?? "Instagram Graph API error");

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            return null;

        var first = data[0];
        return first.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
    }

    public async Task<IReadOnlyList<InstagramMediaSnapshot>> GetRecentHashtagMediaAsync(
        string accessToken,
        string instagramUserId,
        string hashtagId,
        CancellationToken ct)
    {
        var fields =
            "id,caption,media_type,media_url,permalink,thumbnail_url,timestamp,children{media_url,media_type}";
        var url =
            $"https://graph.facebook.com/{ApiVersion}/{Uri.EscapeDataString(hashtagId)}/recent_media?user_id={Uri.EscapeDataString(instagramUserId)}&fields={Uri.EscapeDataString(fields)}&access_token={Uri.EscapeDataString(accessToken)}";

        await using var stream = await SendAndReadStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        if (TryGetErrorMessage(root, out var err))
            throw new GraphApiException(err ?? "Instagram Graph API error");

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return Array.Empty<InstagramMediaSnapshot>();

        var list = new List<InstagramMediaSnapshot>();
        foreach (var item in data.EnumerateArray())
        {
            var snap = MapMedia(item);
            if (snap is not null)
                list.Add(snap);
        }

        return list;
    }

    private async Task<Stream> SendAndReadStreamAsync(string url, CancellationToken ct)
    {
        var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        var stream = await resp.Content.ReadAsStreamAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var raw = doc.RootElement.GetRawText();
            var msg = TryGetErrorMessage(doc.RootElement, out var em) && !string.IsNullOrWhiteSpace(em)
                ? em!
                : raw;
            throw new GraphApiException($"HTTP {(int)resp.StatusCode}: {msg}", raw);
        }

        return stream;
    }

    private static bool TryGetErrorMessage(JsonElement root, out string? message)
    {
        message = null;
        if (!root.TryGetProperty("error", out var err))
            return false;
        if (err.TryGetProperty("message", out var m))
            message = m.GetString();
        return true;
    }

    private static InstagramMediaSnapshot? MapMedia(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var idProp))
            return null;
        var id = idProp.GetString();
        if (string.IsNullOrEmpty(id))
            return null;

        var caption = item.TryGetProperty("caption", out var cap) ? cap.GetString() : null;
        var permalink = item.TryGetProperty("permalink", out var pl) ? pl.GetString() ?? string.Empty : string.Empty;
        var mediaType = item.TryGetProperty("media_type", out var mt) ? mt.GetString() ?? string.Empty : string.Empty;

        string? mediaUrl = item.TryGetProperty("media_url", out var mu) ? mu.GetString() : null;
        string? thumbnail = item.TryGetProperty("thumbnail_url", out var th) ? th.GetString() : null;

        if (string.IsNullOrEmpty(mediaUrl) && item.TryGetProperty("children", out var children))
        {
            if (children.TryGetProperty("data", out var chData) && chData.ValueKind == JsonValueKind.Array && chData.GetArrayLength() > 0)
            {
                var first = chData[0];
                mediaUrl = first.TryGetProperty("media_url", out var cmu) ? cmu.GetString() : mediaUrl;
            }
        }

        DateTimeOffset ts = DateTimeOffset.UtcNow;
        if (item.TryGetProperty("timestamp", out var tsEl))
        {
            if (tsEl.ValueKind == JsonValueKind.String)
                ts = ParseTimestamp(tsEl.GetString());
            else if (tsEl.ValueKind == JsonValueKind.Number && tsEl.TryGetInt64(out var unix))
                ts = DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        if (string.IsNullOrEmpty(permalink))
            permalink = $"https://www.instagram.com/p/{id}/";

        return new InstagramMediaSnapshot(id, caption, mediaUrl, thumbnail, permalink, mediaType, ts);
    }

    private static DateTimeOffset ParseTimestamp(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return DateTimeOffset.UtcNow;
        if (long.TryParse(s, out var unix))
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        if (DateTimeOffset.TryParse(s, out var dto))
            return dto;
        return DateTimeOffset.UtcNow;
    }
}
