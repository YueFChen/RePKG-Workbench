using Microsoft.Win32;
using RePKG.Core.Scanning;

namespace RePKG.App.Services;

public sealed class SteamLocator
{
    private const string WallpaperAppId = "431960";

    private static readonly (RegistryHive Hive, RegistryView View, string Key)[] RegistryLocations =
    [
        (RegistryHive.CurrentUser, RegistryView.Registry64, @"SOFTWARE\Valve\Steam"),
        (RegistryHive.CurrentUser, RegistryView.Registry32, @"SOFTWARE\Valve\Steam"),
        (RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\Valve\Steam"),
        (RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Valve\Steam"),
    ];

    public SteamPaths Detect()
    {
        var steamPath = FindSteamPath();
        if (steamPath is null)
        {
            return new SteamPaths(null, null, null, null, false);
        }

        var libraries = FindSteamAppsDirectories(steamPath);
        foreach (var steamApps in libraries)
        {
            var manifestPath = Path.Combine(steamApps, $"appmanifest_{WallpaperAppId}.acf");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            try
            {
                var manifest = ValveKeyValueParser.Parse(File.ReadAllText(manifestPath));
                var appState = manifest.Children.TryGetValue("AppState", out var value) ? value : manifest;
                var installDirectory = appState.GetValue("installdir");
                if (string.IsNullOrWhiteSpace(installDirectory))
                {
                    continue;
                }

                var wallpaperPath = Path.Combine(steamApps, "common", installDirectory);
                var projectsPath = Path.Combine(wallpaperPath, "projects");
                return new SteamPaths(
                    steamPath,
                    Path.Combine(steamApps, "workshop", "content", WallpaperAppId),
                    projectsPath,
                    Path.Combine(projectsPath, "myprojects"),
                    true);
            }
            catch (Exception exception) when (exception is IOException or FormatException)
            {
                continue;
            }
        }

        return new SteamPaths(
            steamPath,
            Path.Combine(steamPath, "steamapps", "workshop", "content", WallpaperAppId),
            null,
            null,
            false);
    }

    private static string? FindSteamPath()
    {
        foreach (var location in RegistryLocations)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(location.Hive, location.View);
                using var key = baseKey.OpenSubKey(location.Key);
                var path = key?.GetValue("InstallPath") as string;
                if (IsSteamDirectory(path))
                {
                    return Path.GetFullPath(path!);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }
        }

        string[] commonPaths =
        [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
            @"D:\Steam",
            @"E:\Steam",
            @"F:\Steam",
            @"G:\Steam",
        ];
        return commonPaths.FirstOrDefault(IsSteamDirectory);
    }

    private static bool IsSteamDirectory(string? path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && File.Exists(Path.Combine(path, "steam.exe"))
            && Directory.Exists(Path.Combine(path, "steamapps"));
    }

    private static IReadOnlyList<string> FindSteamAppsDirectories(string steamPath)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var primary = Path.Combine(steamPath, "steamapps");
        if (Directory.Exists(primary))
        {
            directories.Add(primary);
        }

        var libraryFile = Path.Combine(primary, "libraryfolders.vdf");
        if (!File.Exists(libraryFile))
        {
            return directories.ToArray();
        }

        try
        {
            var root = ValveKeyValueParser.Parse(File.ReadAllText(libraryFile));
            var libraries = root.Children.TryGetValue("libraryfolders", out var value) ? value : root;
            foreach (var library in libraries.Children.Values)
            {
                var path = library.GetValue("path");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    var steamApps = Path.Combine(path, "steamapps");
                    if (Directory.Exists(steamApps))
                    {
                        directories.Add(Path.GetFullPath(steamApps));
                    }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or FormatException)
        {
            // The primary library remains usable when the optional VDF is malformed.
        }

        return directories.ToArray();
    }
}
