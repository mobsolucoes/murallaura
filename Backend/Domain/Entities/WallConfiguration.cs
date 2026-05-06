namespace HashtagWall.Domain.Entities;

public class WallConfiguration
{
    public Guid Id { get; set; }

    public Guid HashtagConfigurationId { get; set; }
    public HashtagConfiguration HashtagConfiguration { get; set; } = null!;

    public string Title { get; set; } = "Hashtag Wall";

    /// <summary>Optional URL or path to logo image served by admin/uploads.</summary>
    public string? LogoUrl { get; set; }

    public string PrimaryColor { get; set; } = "#6366f1";
    public string SecondaryColor { get; set; } = "#1e1b4b";
    public string AccentColor { get; set; } = "#f472b6";

    /// <summary>dark | light</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>Seconds each slide stays in focus.</summary>
    public int DisplayDurationSeconds { get; set; } = 30;

    public bool ShowCaption { get; set; } = true;
    public bool ShowQrCode { get; set; } = true;

    public DateTimeOffset UpdatedAt { get; set; }
}
