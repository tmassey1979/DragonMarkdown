namespace DragonMarkdown.Core.Rendering;

public sealed record MarkdownRenderResult(
    string Html,
    IReadOnlyList<BlockedMarkdownReference> BlockedReferences);
