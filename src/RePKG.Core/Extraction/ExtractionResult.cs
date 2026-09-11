namespace RePKG.Core.Extraction;

public sealed record ExtractionResult(
    bool IsSuccess,
    int? ExitCode,
    TimeSpan Elapsed,
    string? ErrorMessage = null,
    string? OutputPath = null);
