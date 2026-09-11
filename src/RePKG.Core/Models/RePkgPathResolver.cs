namespace RePKG.Core.Models;

public static class RePkgPathResolver
{
    public static string BundledPath(string? baseDirectory = null)
    {
        return Path.Combine(baseDirectory ?? AppContext.BaseDirectory, "tools", "RePKG.exe");
    }

    public static string Resolve(string? configuredPath, string? baseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        return BundledPath(baseDirectory);
    }
}
