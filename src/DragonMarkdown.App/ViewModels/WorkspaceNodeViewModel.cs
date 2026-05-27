using System.Collections.ObjectModel;
using DragonMarkdown.Core.Workspaces;

namespace DragonMarkdown.App.ViewModels;

public sealed class WorkspaceNodeViewModel
{
    public WorkspaceNodeViewModel(
        string name,
        string fullPath,
        string relativePath,
        WorkspaceNodeKind kind,
        IEnumerable<WorkspaceNodeViewModel>? children = null)
    {
        Name = name;
        FullPath = fullPath;
        RelativePath = relativePath;
        Kind = kind;
        Children = new ObservableCollection<WorkspaceNodeViewModel>(children ?? []);
    }

    public string Name { get; }

    public string FullPath { get; }

    public string RelativePath { get; }

    public WorkspaceNodeKind Kind { get; }

    public ObservableCollection<WorkspaceNodeViewModel> Children { get; }

    public string Glyph => Kind switch
    {
        WorkspaceNodeKind.Folder => "DIR",
        WorkspaceNodeKind.Markdown => "MD",
        _ => "FILE"
    };

    public static WorkspaceNodeViewModel FromWorkspaceItem(WorkspaceItem item)
    {
        var kind = item.Kind switch
        {
            WorkspaceItemKind.Folder => WorkspaceNodeKind.Folder,
            WorkspaceItemKind.MarkdownFile => WorkspaceNodeKind.Markdown,
            _ => WorkspaceNodeKind.Asset
        };

        return new WorkspaceNodeViewModel(
            item.Name,
            item.FullPath,
            item.RelativePath,
            kind,
            item.Children.Select(FromWorkspaceItem));
    }
}
