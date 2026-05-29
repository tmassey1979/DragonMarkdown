namespace DragonMarkdown.Core.Health;

public sealed record WorkspaceHealthReport(IReadOnlyList<WorkspaceHealthIssue> Issues)
{
    public int ErrorCount => Issues.Count(issue => issue.Severity == WorkspaceHealthIssueSeverity.Error);

    public int WarningCount => Issues.Count(issue => issue.Severity == WorkspaceHealthIssueSeverity.Warning);

    public int InfoCount => Issues.Count(issue => issue.Severity == WorkspaceHealthIssueSeverity.Info);

    public bool IsHealthy => ErrorCount == 0 && WarningCount == 0;

    public static WorkspaceHealthReport Empty { get; } = new([]);
}
