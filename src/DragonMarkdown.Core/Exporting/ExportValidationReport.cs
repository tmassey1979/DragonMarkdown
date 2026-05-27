namespace DragonMarkdown.Core.Exporting;

public sealed record ExportValidationReport(
    IReadOnlyList<ExportValidationIssue> Errors,
    IReadOnlyList<ExportValidationIssue> Warnings)
{
    public bool IsValid => Errors.Count == 0;

    public static ExportValidationReport Empty { get; } = new([], []);
}
