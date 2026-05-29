using DragonMarkdown.Core.Exporting;

namespace DragonMarkdown.App.ViewModels;

public sealed class ExportValidationIssueViewModel
{
    public ExportValidationIssueViewModel(ExportValidationIssue issue, string severity)
    {
        Issue = issue;
        Severity = severity;
    }

    public ExportValidationIssue Issue { get; }

    public string Severity { get; }

    public string Code => Issue.Code;

    public string Message => Issue.Message;

    public string Reference => Issue.Reference;
}
