namespace HashtagWall.Worker;

public sealed class InstagramWebRpaOptions
{
    public const string SectionName = "InstagramWebRpa";

    public bool Enabled { get; set; }
    public bool Headless { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int MaxPostsPerRun { get; set; } = 20;
    public int NavigationTimeoutMs { get; set; } = 45000;
}
