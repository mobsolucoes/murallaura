using System.IO.Compression;
using HashtagWall.Domain.Entities;

namespace HashtagWall.Api.Services;

public sealed class MediaZipExportService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;

    public MediaZipExportService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
    {
        _env = env;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<byte[]?> BuildZipAsync(IReadOnlyList<InstagramMediaPost> posts, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(nameof(MediaZipExportService));
        client.Timeout = TimeSpan.FromMinutes(10);

        using var ms = new MemoryStream();
        var added = 0;

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var post in posts)
            {
                var sourceUrl = post.MediaUrl ?? post.ThumbnailUrl;
                if (string.IsNullOrWhiteSpace(sourceUrl))
                    continue;

                var bytes = await TryGetBytesAsync(client, sourceUrl, ct);
                if (bytes is null || bytes.Length == 0)
                    continue;

                added++;
                var ext = GuessExtension(sourceUrl, bytes);
                var safeId = SanitizeFileName(post.InstagramMediaId);
                var entryName = $"{added:000}_{safeId}{ext}";
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(bytes, ct);
            }
        }

        return added == 0 ? null : ms.ToArray();
    }

    private async Task<byte[]?> TryGetBytesAsync(HttpClient client, string url, CancellationToken ct)
    {
        if (TryResolveLocalUploadPath(url, out var localPath) && System.IO.File.Exists(localPath))
            return await System.IO.File.ReadAllBytesAsync(localPath, ct);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        if (TryResolveLocalUploadPath(uri.AbsolutePath, out localPath) && System.IO.File.Exists(localPath))
            return await System.IO.File.ReadAllBytesAsync(localPath, ct);

        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private bool TryResolveLocalUploadPath(string urlOrPath, out string fullPath)
    {
        fullPath = string.Empty;
        var path = urlOrPath;
        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
            path = uri.AbsolutePath;

        const string prefix = "/uploads/";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var fileName = path[prefix.Length..];
        if (fileName.Contains("..", StringComparison.Ordinal))
            return false;

        var uploadsRoot = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "uploads");
        fullPath = Path.Combine(uploadsRoot, fileName);
        return true;
    }

    private static string GuessExtension(string url, byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return ".jpg";
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50)
            return ".png";
        if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49)
            return ".webp";

        var ext = Path.GetExtension(url);
        if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 5)
            return ext.ToLowerInvariant();

        return ".jpg";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "media" : sanitized[..Math.Min(sanitized.Length, 80)];
    }
}
