namespace DragonMarkdown.Core.Rendering;

public sealed record BlockedMarkdownReference(
    string Reference,
    MarkdownReferenceKind Kind,
    MarkdownReferenceBlockReason Reason);
