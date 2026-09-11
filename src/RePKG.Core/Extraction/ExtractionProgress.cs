namespace RePKG.Core.Extraction;

public sealed record ExtractionProgress(
    int CompletedCount,
    int TotalCount,
    string? CurrentItem,
    string? Message)
{
    public double Percentage => TotalCount == 0
        ? 0
        : (double)CompletedCount / TotalCount * 100;
}
