namespace HashtagWall.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "HashtagWall";
    public string Audience { get; set; } = "HashtagWall";
    public string Key { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 60;
}
