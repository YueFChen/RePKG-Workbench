using RePKG.Core.Scanning;

namespace RePKG.Core.Abstractions;

public interface IProjectScanner
{
    Task<ScanResult> ScanAsync(
        string rootPath,
        bool recursive,
        CancellationToken cancellationToken = default);
}
