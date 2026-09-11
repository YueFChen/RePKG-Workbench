namespace RePKG.Core.Models;

public sealed record AppearanceSettings
{
    public string Theme { get; init; } = "Dark";

    public string BackgroundImagePath { get; init; } = string.Empty;

    public double BackgroundOpacity { get; init; } = 0.35;
}
