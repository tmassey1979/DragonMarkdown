namespace DragonMarkdown.Core.Health;

public static class WorkspaceHealthIssueCodes
{
    public const string MissingImage = nameof(MissingImage);
    public const string BrokenLink = nameof(BrokenLink);
    public const string DuplicateHeading = nameof(DuplicateHeading);
    public const string MalformedFrontMatter = nameof(MalformedFrontMatter);
    public const string UnsupportedMermaid = nameof(UnsupportedMermaid);
    public const string OrphanDocument = nameof(OrphanDocument);
    public const string DeadAsset = nameof(DeadAsset);
    public const string DuplicateHeadingSlug = nameof(DuplicateHeadingSlug);
}
