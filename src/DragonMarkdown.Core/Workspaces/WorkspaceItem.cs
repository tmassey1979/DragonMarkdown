namespace DragonMarkdown.Core.Workspaces;

public enum WorkspaceItemKind
{
    Folder,
    MarkdownFile,
    AssetFile
}

public sealed class WorkspaceItem
{
    public WorkspaceItem(
        string name,
        string fullPath,
        string relativePath,
        WorkspaceItemKind kind,
        IEnumerable<WorkspaceItem>? children = null)
    {
        Name = name;
        FullPath = fullPath;
        RelativePath = relativePath;
        Kind = kind;
        Children = (children ?? []).ToArray();
    }

    public string Name { get; }

    public string FullPath { get; }

    public string RelativePath { get; }

    public WorkspaceItemKind Kind { get; }

    public IReadOnlyList<WorkspaceItem> Children { get; }
}
