namespace DragonMarkdown.Core.Documents;

public sealed record MarkdownDocumentStatistics(
    int WordCount,
    int HeadingCount,
    int LinkCount,
    int ImageCount,
    int CodeBlockCount,
    int EstimatedReadingMinutes)
{
    public static MarkdownDocumentStatistics Empty { get; } = new(0, 0, 0, 0, 0, 0);
}
