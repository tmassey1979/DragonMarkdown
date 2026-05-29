namespace DragonMarkdown.Core.Workspaces;

public sealed record WorkspaceStatistics(
    int DocumentCount,
    int WordCount,
    int HeadingCount,
    int LinkCount,
    int ImageCount,
    int EstimatedReadingMinutes)
{
    public static WorkspaceStatistics Empty { get; } = new(0, 0, 0, 0, 0, 0);
}
