namespace DragonMarkdown.Core.Rendering;

public sealed record MarkdownRenderResult(
    string Html,
    IReadOnlyList<BlockedMarkdownReference> BlockedReferences);

public sealed record BlockedMarkdownReference(
    string Reference,
    MarkdownReferenceKind Kind,
    MarkdownReferenceBlockReason Reason);

public enum MarkdownReferenceKind
{
    Link,
    Image
}

public enum MarkdownReferenceBlockReason
{
    OutsideWorkspace,
    RawLocalPath
}
