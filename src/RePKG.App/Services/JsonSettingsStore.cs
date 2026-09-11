using System.Text.Json;
using RePKG.App.Localization;
using RePKG.Core.Abstractions;
using RePKG.Core.Models;

namespace RePKG.App.Services;

public sealed class JsonSettingsStore : ISettingsStore
{
    private const string ApplicationDataDirectoryName = "RePKG-Workbench";
    private const string LegacyApplicationDataDirectoryName = "RePKG-GUI";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string? _legacySettingsPath;

    public JsonSettingsStore(string? settingsPath = null)
    {
        if (settingsPath is not null)
        {
            SettingsPath = settingsPath;
            return;
        }

        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        SettingsPath = Path.Combine(localApplicationData, ApplicationDataDirectoryName, "settings.json");
        _legacySettingsPath = Path.Combine(localApplicationData, LegacyApplicationDataDirectoryName, "settings.json");
    }

    public string SettingsPath { get; }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var sourcePath = File.Exists(SettingsPath)
            ? SettingsPath
            : _legacySettingsPath is not null && File.Exists(_legacySettingsPath)
                ? _legacySettingsPath
                : null;
        if (sourcePath is null)
        {
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(sourcePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken)
                ?? new AppSettings();
            if (!string.Equals(sourcePath, SettingsPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await SaveAsync(settings, cancellationToken);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // Loading legacy settings is more important than completing the one-time migration.
                }
            }

            return settings;
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            var backupPath = $"{sourcePath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(sourcePath, backupPath, true);
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException(LocalizationService.Current.Get(
                "Error.SettingsDirectory",
                "The settings directory could not be determined."));
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{SettingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, SettingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
