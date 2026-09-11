using System.Diagnostics;
using RePKG.App.Localization;

namespace RePKG.App.Services;

public sealed class ExplorerService
{
    public void OpenLocation(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException(
                LocalizationService.Current.Get("Error.PathNotFound", "The path does not exist."),
                fullPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = false,
        };
        if (File.Exists(fullPath))
        {
            startInfo.ArgumentList.Add($"/select,{fullPath}");
        }
        else
        {
            startInfo.ArgumentList.Add(fullPath);
        }

        Process.Start(startInfo)?.Dispose();
    }
}
