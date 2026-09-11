using RePKG.App.Localization;

namespace RePKG.App.Services;

public sealed class BackgroundService
{
    private static readonly HashSet<string> SupportedExtensions =
        new([".png", ".jpg", ".jpeg", ".webp", ".bmp"], StringComparer.OrdinalIgnoreCase);

    public BackgroundService(string? backgroundDirectory = null)
    {
        BackgroundDirectory = backgroundDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RePKG-Workbench",
            "Backgrounds");
    }

    public string BackgroundDirectory { get; }

    public async Task<string> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                LocalizationService.Current.Get("Error.BackgroundNotFound", "The background image does not exist."),
                sourcePath);
        }

        var extension = Path.GetExtension(sourcePath);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException(LocalizationService.Current.Get(
                "Error.BackgroundFormat",
                "Background images must be PNG, JPG, JPEG, WebP, or BMP."));
        }

        Directory.CreateDirectory(BackgroundDirectory);
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var destination = Path.Combine(BackgroundDirectory, $"{baseName}{extension}");
        var suffix = 1;
        while (File.Exists(destination) && !FilesMatch(sourcePath, destination))
        {
            destination = Path.Combine(BackgroundDirectory, $"{baseName}_{suffix++}{extension}");
        }

        if (!File.Exists(destination))
        {
            await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await source.CopyToAsync(target, cancellationToken);
        }

        return destination;
    }

    private static bool FilesMatch(string left, string right)
    {
        return new FileInfo(left).Length == new FileInfo(right).Length;
    }
}
