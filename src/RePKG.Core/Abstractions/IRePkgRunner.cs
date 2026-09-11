using RePKG.Core.Extraction;
using RePKG.Core.Models;

namespace RePKG.Core.Abstractions;

public interface IRePkgRunner
{
    Task<ExtractionResult> ExtractAsync(
        ProjectItem project,
        string outputRoot,
        ExtractionOptions options,
        IProgress<ExtractionProgress> progress,
        CancellationToken cancellationToken);
}
