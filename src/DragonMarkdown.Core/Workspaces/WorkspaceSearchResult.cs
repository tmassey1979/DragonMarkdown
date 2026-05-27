namespace DragonMarkdown.Core.Workspaces;

public enum WorkspaceSearchMatchKind
{
    Path,
    Title,
    Content
}

public sealed record WorkspaceSearchResult(
    string FullPath,
    string RelativePath,
    string Title,
    WorkspaceSearchMatchKind MatchKind,
    string Preview);
