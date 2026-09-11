using System.Diagnostics;
using RePKG.Core.Models;

namespace RePKG.Core.Extraction;

public sealed class DirectoryProjectCopier
{
    public Task<ExtractionResult> CopyAsync(
        ProjectItem project,
        string outputRoot,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var outputPath = PathSafety.GetContainedPath(outputRoot, project.DirectoryName);
        return Task.Run(() => Copy(project.DirectoryPath, outputPath, overwrite, cancellationToken), cancellationToken);
    }

    private static ExtractionResult Copy(
        string sourcePath,
        string outputPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.StartNew();
        try
        {
            if (Directory.Exists(outputPath))
            {
                if (!overwrite)
                {
                    return new ExtractionResult(true, null, started.Elapsed, null, outputPath);
                }

                if (File.GetAttributes(outputPath).HasFlag(FileAttributes.ReparsePoint))
                {
                    return new ExtractionResult(false, null, started.Elapsed, "Refusing to overwrite a reparse-point directory.", outputPath);
                }

                Directory.Delete(outputPath, true);
            }

            CopyDirectory(sourcePath, outputPath, cancellationToken);
            return new ExtractionResult(true, null, started.Elapsed, null, outputPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new ExtractionResult(false, null, started.Elapsed, exception.Message, outputPath);
        }
    }

    private static void CopyDirectory(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationPath);
        foreach (var file in Directory.EnumerateFiles(sourcePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)), true);
        }

        foreach (var directory in Directory.EnumerateDirectories(sourcePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.GetAttributes(directory).HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            CopyDirectory(directory, Path.Combine(destinationPath, Path.GetFileName(directory)), cancellationToken);
        }
    }
}
