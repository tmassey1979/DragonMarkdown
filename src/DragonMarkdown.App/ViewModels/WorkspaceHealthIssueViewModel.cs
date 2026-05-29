using DragonMarkdown.Core.Health;

namespace DragonMarkdown.App.ViewModels;

public sealed class WorkspaceHealthIssueViewModel
{
    public WorkspaceHealthIssueViewModel(WorkspaceHealthIssue issue, string workspaceRoot)
    {
        Issue = issue;
        RelativePath = Path.GetRelativePath(workspaceRoot, issue.DocumentPath);
    }

    public WorkspaceHealthIssue Issue { get; }

    public string Code => Issue.Code;

    public string Severity => Issue.Severity.ToString();

    public string Message => Issue.Message;

    public string DocumentPath => Issue.DocumentPath;

    public string RelativePath { get; }

    public string? Reference => Issue.Reference;
}
