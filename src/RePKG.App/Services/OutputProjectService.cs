using RePKG.App.Localization;
using RePKG.Core.Models;

namespace RePKG.App.Services;

public sealed class OutputProjectService
{
    public Task<int> DeleteAsync(
        string outputRoot,
        IReadOnlyCollection<ProjectItem> projects,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var root = Path.GetFullPath(outputRoot);
            var deleted = 0;
            foreach (var project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target = Path.GetFullPath(project.DirectoryPath);
                PathSafety.EnsureContained(root, target);
                if (!Directory.Exists(target))
                {
                    continue;
                }

                if (File.GetAttributes(target).HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new InvalidOperationException(LocalizationService.Current.Format(
                        "Error.DeleteReparsePoint",
                        "Refusing to delete a reparse point: {0}",
                        target));
                }

                Directory.Delete(target, true);
                deleted++;
            }

            return deleted;
        }, cancellationToken);
    }
}
