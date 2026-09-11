namespace RePKG.Core.Models;

public sealed record ProjectItem(
    string DirectoryPath,
    string? PackagePath,
    string Title,
    ProjectType Type,
    string? PreviewPath,
    long Size,
    string? WorkshopId,
    string? Description = null,
    IReadOnlyList<string>? Tags = null,
    int? Version = null,
    string? WorkshopUrl = null,
    string? SchemeColor = null,
    string? ContentRating = null,
    string? VideoPath = null)
{
    public string DirectoryName => Path.GetFileName(
        DirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
