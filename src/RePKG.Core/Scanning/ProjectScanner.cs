using System.Text.Json;
using RePKG.Core.Abstractions;
using RePKG.Core.Models;

namespace RePKG.Core.Scanning;

public sealed class ProjectScanner : IProjectScanner
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp"];
    private static readonly string[] VideoExtensions = [".mp4", ".webm", ".mkv", ".avi", ".mov", ".wmv", ".flv"];
    private static readonly string[] PreviewNames = ["preview", "thumb", "thumbnail", "cover", "icon"];

    public Task<ScanResult> ScanAsync(
        string rootPath,
        bool recursive,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Scan(rootPath, recursive, cancellationToken), cancellationToken);
    }

    private static ScanResult Scan(string rootPath, bool recursive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"The directory does not exist: {root}");
        }

        var projects = new List<ProjectItem>();
        var warnings = new List<ScanWarning>();
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();

            string[] files;
            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                warnings.Add(new ScanWarning(directory, exception.Message));
                continue;
            }

            var projectFile = files.FirstOrDefault(file =>
                string.Equals(Path.GetFileName(file), "project.json", StringComparison.OrdinalIgnoreCase));

            if (projectFile is not null)
            {
                var project = TryReadProject(directory, projectFile, files, warnings, cancellationToken);
                if (project is not null)
                {
                    projects.Add(project);
                }
            }

            if (!recursive)
            {
                continue;
            }

            try
            {
                foreach (var child in Directory.EnumerateDirectories(directory))
                {
                    pending.Push(child);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                warnings.Add(new ScanWarning(directory, exception.Message));
            }
        }

        projects.Sort((left, right) => string.Compare(left.Title, right.Title, StringComparison.CurrentCultureIgnoreCase));
        return new ScanResult(projects, warnings);
    }

    private static ProjectItem? TryReadProject(
        string directory,
        string projectFile,
        IReadOnlyList<string> files,
        ICollection<ScanWarning> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(projectFile);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            var root = document.RootElement;
            var typeText = GetString(root, "type");
            if (!TryParseType(typeText, out var type))
            {
                warnings.Add(new ScanWarning(projectFile, $"Unsupported project type: {typeText ?? "<empty>"}"));
                return null;
            }

            var packagePath = type == ProjectType.Scene
                ? files.FirstOrDefault(file => string.Equals(Path.GetExtension(file), ".pkg", StringComparison.OrdinalIgnoreCase))
                : null;
            var videoPath = type == ProjectType.Video ? FindVideo(root, directory, files) : null;
            var title = GetString(root, "title") ?? GetString(root, "name") ?? Path.GetFileName(directory);
            var previewPath = FindPreview(root, directory, files);
            var size = type == ProjectType.Scene && packagePath is not null
                ? new FileInfo(packagePath).Length
                : GetDirectorySize(directory, warnings, cancellationToken);

            return new ProjectItem(
                Path.GetFullPath(directory),
                packagePath,
                title,
                type,
                previewPath,
                size,
                GetString(root, "workshopid"),
                GetString(root, "description"),
                GetStringArray(root, "tags"),
                GetInt32(root, "version"),
                GetString(root, "workshopurl"),
                GetNestedPropertyValue(root, "schemecolor"),
                GetString(root, "contentrating"),
                videoPath);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            warnings.Add(new ScanWarning(projectFile, exception.Message));
            return null;
        }
    }

    private static bool TryParseType(string? value, out ProjectType type)
    {
        return Enum.TryParse(value, true, out type) && Enum.IsDefined(type);
    }

    private static string? FindPreview(JsonElement root, string directory, IReadOnlyList<string> files)
    {
        var configured = GetString(root, "preview");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, configured));
            if (File.Exists(candidate) && IsWithin(directory, candidate))
            {
                return candidate;
            }

            var basePath = Path.Combine(directory, Path.ChangeExtension(configured, null));
            foreach (var extension in ImageExtensions)
            {
                candidate = basePath + extension;
                if (File.Exists(candidate) && IsWithin(directory, candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        var directoryName = Path.GetFileName(directory);
        return FindFirstFile(files, directoryName, ImageExtensions)
            ?? PreviewNames.Select(name => FindFirstFile(files, name, ImageExtensions)).FirstOrDefault(path => path is not null);
    }

    private static string? FindVideo(JsonElement root, string directory, IReadOnlyList<string> files)
    {
        var configured = GetNestedPropertyValue(root, "video") ?? GetString(root, "file");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, configured));
            if (File.Exists(candidate) && IsWithin(directory, candidate))
            {
                return candidate;
            }
        }

        return files.FirstOrDefault(file => VideoExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
    }

    private static string? FindFirstFile(IEnumerable<string> files, string baseName, IEnumerable<string> extensions)
    {
        return files.FirstOrDefault(file =>
            string.Equals(Path.GetFileNameWithoutExtension(file), baseName, StringComparison.OrdinalIgnoreCase)
            && extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
    }

    private static long GetDirectorySize(string directory, ICollection<ScanWarning> warnings, CancellationToken cancellationToken)
    {
        long total = 0;
        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();
            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    total += new FileInfo(file).Length;
                }

                foreach (var child in Directory.EnumerateDirectories(current))
                {
                    pending.Push(child);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                warnings.Add(new ScanWarning(current, exception.Message));
            }
        }

        return total;
    }

    private static bool IsWithin(string root, string candidate)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(candidate));
        return relative != ".."
            && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static int? GetInt32(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : null;
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private static string? GetNestedPropertyValue(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty("general", out var general)
            || !general.TryGetProperty("properties", out var properties)
            || !properties.TryGetProperty(propertyName, out var property)
            || !property.TryGetProperty("value", out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
