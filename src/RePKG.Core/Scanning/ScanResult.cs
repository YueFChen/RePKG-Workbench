using RePKG.Core.Models;

namespace RePKG.Core.Scanning;

public sealed record ScanResult(
    IReadOnlyList<ProjectItem> Projects,
    IReadOnlyList<ScanWarning> Warnings)
{
    public long TotalSize => Projects.Sum(project => project.Size);
}
