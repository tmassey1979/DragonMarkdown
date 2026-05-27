namespace DragonMarkdown.Core.Exporting;

public sealed record ExportValidationIssue(
    string Code,
    string Message,
    string Reference);
