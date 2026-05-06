namespace HashtagWall.Application.DTOs;

public record WallConfigurationDto(
    Guid Id,
    Guid HashtagConfigurationId,
    string Title,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string Theme,
    int DisplayDurationSeconds,
    bool ShowCaption,
    bool ShowQrCode);

public record UpdateWallConfigurationRequest(
    string Title,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string Theme,
    int DisplayDurationSeconds,
    bool ShowCaption,
    bool ShowQrCode);
