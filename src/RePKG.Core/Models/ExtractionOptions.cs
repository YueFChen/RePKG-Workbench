namespace RePKG.Core.Models;

public sealed record ExtractionOptions(
    bool ConvertTex = true,
    bool CopyProject = true,
    bool Overwrite = false,
    bool CopyPreview = true);
