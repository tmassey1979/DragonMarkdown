using DragonMarkdown.Core.Workspaces;

namespace DragonMarkdown.App.ViewModels;

public sealed class DocumentBacklinkViewModel
{
    public DocumentBacklinkViewModel(WorkspaceBacklink backlink)
    {
        Backlink = backlink;
    }

    public WorkspaceBacklink Backlink { get; }

    public string FullPath => Backlink.FullPath;

    public string RelativePath => Backlink.RelativePath;

    public string Title => Backlink.Title;

    public string Preview => Backlink.Preview;
}
