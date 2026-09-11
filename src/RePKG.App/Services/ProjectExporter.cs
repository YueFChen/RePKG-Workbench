using System.Diagnostics;
using System.IO;
using RePKG.App.Localization;
using RePKG.Core.Abstractions;
using RePKG.Core.Extraction;
using RePKG.Core.Models;

namespace RePKG.App.Services;

public sealed class ProjectExporter(string rePkgPath, TimeSpan? timeout = null) : IRePkgRunner
{
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromMinutes(5);
    private readonly DirectoryProjectCopier _directoryCopier = new();

    public async Task<ExtractionResult> ExtractAsync(
        ProjectItem project,
        string outputRoot,
        ExtractionOptions options,
        IProgress<ExtractionProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(progress);

        var outputPath = PathSafety.GetContainedPath(outputRoot, project.DirectoryName);
        if (project.Type == ProjectType.Scene)
        {
            return await ExtractSceneAsync(project, outputPath, options, progress, cancellationToken);
        }

        var result = await _directoryCopier.CopyAsync(project, outputRoot, options.Overwrite, cancellationToken);
        if (result.IsSuccess && Directory.Exists(outputPath) && !options.Overwrite)
        {
            progress.Report(new ExtractionProgress(
                0,
                1,
                project.Title,
                LocalizationService.Current.Get("Export.DirectoryReady", "The project directory was copied or already exists.")));
        }

        return result;
    }

    private async Task<ExtractionResult> ExtractSceneAsync(
        ProjectItem project,
        string outputPath,
        ExtractionOptions options,
        IProgress<ExtractionProgress> progress,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.StartNew();
        if (!File.Exists(rePkgPath))
        {
            return new ExtractionResult(
                false,
                null,
                started.Elapsed,
                LocalizationService.Current.Format("Export.RePkgNotFound", "RePKG.exe was not found: {0}", rePkgPath),
                outputPath);
        }

        if (project.PackagePath is null || !File.Exists(project.PackagePath))
        {
            return new ExtractionResult(
                false,
                null,
                started.Elapsed,
                LocalizationService.Current.Get("Export.PackageMissing", "The scene project does not contain a usable .pkg file."),
                outputPath);
        }

        Directory.CreateDirectory(outputPath);
        var startInfo = new ProcessStartInfo
        {
            FileName = rePkgPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("extract");
        if (options.ConvertTex)
        {
            startInfo.ArgumentList.Add("-t");
        }

        if (options.CopyProject)
        {
            startInfo.ArgumentList.Add("-c");
        }

        if (options.Overwrite)
        {
            startInfo.ArgumentList.Add("--overwrite");
        }

        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputPath);
        startInfo.ArgumentList.Add(project.PackagePath);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return new ExtractionResult(
                    false,
                    null,
                    started.Elapsed,
                    LocalizationService.Current.Get("Export.StartFailed", "The RePKG process could not be started."),
                    outputPath);
            }

            using var timeoutSource = new CancellationTokenSource(_timeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            var outputTask = ReadLinesAsync(process.StandardOutput, "RePKG", progress, linkedSource.Token);
            var errorTask = ReadLinesAsync(
                process.StandardError,
                LocalizationService.Current.Get("Export.ErrorSource", "RePKG error"),
                progress,
                linkedSource.Token);

            try
            {
                await process.WaitForExitAsync(linkedSource.Token);
                await Task.WhenAll(outputTask, errorTask);
            }
            catch (OperationCanceledException)
            {
                KillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                return new ExtractionResult(
                    false,
                    process.ExitCode,
                    started.Elapsed,
                    LocalizationService.Current.Format("Export.Timeout", "RePKG timed out after {0:0} seconds.", _timeout.TotalSeconds),
                    outputPath);
            }

            if (process.ExitCode != 0)
            {
                return new ExtractionResult(
                    false,
                    process.ExitCode,
                    started.Elapsed,
                    LocalizationService.Current.Format("Export.ExitCode", "RePKG exited with code {0}.", process.ExitCode),
                    outputPath);
            }

            if (options.CopyPreview && project.PreviewPath is not null)
            {
                CopyPreview(project.PreviewPath, outputPath, options.Overwrite);
            }

            return new ExtractionResult(true, process.ExitCode, started.Elapsed, null, outputPath);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new ExtractionResult(false, null, started.Elapsed, exception.Message, outputPath);
        }
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        string source,
        IProgress<ExtractionProgress> progress,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                progress.Report(new ExtractionProgress(0, 1, null, $"{source}: {line}"));
            }
        }
    }

    private static void CopyPreview(string previewPath, string outputPath, bool overwrite)
    {
        if (!File.Exists(previewPath))
        {
            return;
        }

        var destination = Path.Combine(outputPath, Path.GetFileName(previewPath));
        if (!File.Exists(destination) || overwrite)
        {
            File.Copy(previewPath, destination, overwrite);
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between HasExited and Kill.
        }
    }
}
