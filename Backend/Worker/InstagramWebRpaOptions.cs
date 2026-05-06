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
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialBackoffSeconds { get; set; } = 5;
    public int PauseAfterLoginMs { get; set; } = 2500;
    public string SessionStatePath { get; set; } = "/tmp/instagram-rpa-state.json";
    public bool FailOnCheckpointOrTwoFactor { get; set; } = true;
}
