namespace RePKG.Core.Models;

public static class PathSafety
{
    public static string GetContainedPath(string rootPath, string childName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(childName);
        var root = Path.GetFullPath(rootPath);
        var target = Path.GetFullPath(Path.Combine(root, childName));
        EnsureContained(root, target);
        return target;
    }

    public static void EnsureContained(string rootPath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        var root = Path.GetFullPath(rootPath);
        var target = Path.GetFullPath(targetPath);
        var relative = Path.GetRelativePath(root, target);
        if (relative == "."
            || relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"The target is outside the allowed root directory: {target}");
        }
    }
}
