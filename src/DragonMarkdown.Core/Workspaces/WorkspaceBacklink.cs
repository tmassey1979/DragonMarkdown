namespace DragonMarkdown.Core.Workspaces;

public sealed record WorkspaceBacklink(
    string FullPath,
    string RelativePath,
    string Title,
    string Preview);
