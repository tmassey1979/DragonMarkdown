namespace DragonMarkdown.Core.Rendering;

public sealed class MarkdownRenderOptions
{
    public MarkdownRenderOptions(string workspaceRootPath, string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        WorkspaceRootPath = Path.GetFullPath(workspaceRootPath);
        DocumentPath = Path.GetFullPath(documentPath);
    }

    public string WorkspaceRootPath { get; }

    public string DocumentPath { get; }

    public string AppUrlScheme { get; init; } = "dragonmarkdown";

    public string WorkspaceHost { get; init; } = "workspace";
}
