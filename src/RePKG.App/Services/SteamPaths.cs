namespace RePKG.App.Services;

public sealed record SteamPaths(
    string? SteamPath,
    string? WorkshopPath,
    string? WallpaperProjectsPath,
    string? MyProjectsPath,
    bool WallpaperEngineFound);
