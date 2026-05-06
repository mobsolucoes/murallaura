namespace HashtagWall.Infrastructure.Services;

public static class HashtagNormalizer
{
    public static string Normalize(string input)
    {
        var s = input.Trim();
        if (s.StartsWith("#", StringComparison.Ordinal))
            s = s[1..];
        return s.ToLowerInvariant();
    }
}
