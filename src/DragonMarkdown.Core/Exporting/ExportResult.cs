namespace DragonMarkdown.Core.Exporting;

public sealed record ExportResult(
    bool Succeeded,
    string OutputPath,
    ExportValidationReport ValidationReport,
    string? ErrorMessage = null)
{
    public static ExportResult Success(string outputPath, ExportValidationReport validationReport) =>
        new(true, outputPath, validationReport);

    public static ExportResult Failure(string outputPath, ExportValidationReport validationReport, string errorMessage) =>
        new(false, outputPath, validationReport, errorMessage);
}
