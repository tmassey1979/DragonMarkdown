namespace DragonMarkdown.Core.Health;

public sealed record WorkspaceHealthIssue(
    string Code,
    WorkspaceHealthIssueSeverity Severity,
    string Message,
    string DocumentPath,
    string? Reference = null,
    int? LineNumber = null);
