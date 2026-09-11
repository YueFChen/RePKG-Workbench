namespace RePKG.Core.Models;

public sealed record AppSettings
{
    public string Language { get; init; } = string.Empty;

    public string InputPath { get; init; } = string.Empty;

    public string OutputPath { get; init; } = string.Empty;

    public string RePkgPath { get; init; } = RePkgPathResolver.BundledPath();

    public bool RecursiveScan { get; init; } = true;

    public ExtractionOptions Extraction { get; init; } = new();

    public AppearanceSettings Appearance { get; init; } = new();
}
