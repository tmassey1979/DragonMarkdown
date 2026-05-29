using System.Text;
using System.Text.RegularExpressions;

namespace DragonMarkdown.Core.Documents;

public sealed partial class MarkdownDocumentStatisticsService
{
    private const int WordsPerMinute = 225;

    public MarkdownDocumentStatistics Analyze(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return MarkdownDocumentStatistics.Empty;
        }

        string withoutFrontMatter = StripFrontMatter(markdown);
        var codeBlockCount = CodeFenceRegex().Matches(withoutFrontMatter).Count;
        string visibleMarkdown = StripCodeFences(withoutFrontMatter);
        var wordCount = WordRegex()
            .Matches(StripMarkdownSyntax(StripInlineReferences(visibleMarkdown)))
            .Count;

        return new MarkdownDocumentStatistics(
            wordCount,
            HeadingRegex().Matches(visibleMarkdown).Count,
            LinkRegex().Matches(visibleMarkdown).Count,
            ImageRegex().Matches(visibleMarkdown).Count,
            codeBlockCount,
            CalculateReadingMinutes(wordCount));
    }

    private static int CalculateReadingMinutes(int wordCount)
    {
        if (wordCount == 0)
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    private static string StripFrontMatter(string markdown)
    {
        string normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return markdown;
        }

        int closingIndex = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        return closingIndex < 0 ? markdown : normalized[(closingIndex + 5)..];
    }

    private static string StripCodeFences(string markdown) =>
        CodeFenceRegex().Replace(markdown, string.Empty);

    private static string StripInlineReferences(string markdown) =>
        LinkRegex().Replace(ImageRegex().Replace(markdown, string.Empty), string.Empty);

    private static string StripMarkdownSyntax(string markdown)
    {
        var builder = new StringBuilder(markdown);
        builder.Replace("#", " ");
        builder.Replace("*", " ");
        builder.Replace("_", " ");
        builder.Replace("`", " ");
        builder.Replace("[", " ");
        builder.Replace("]", " ");
        builder.Replace("(", " ");
        builder.Replace(")", " ");
        builder.Replace("!", " ");
        return builder.ToString();
    }

    [GeneratedRegex(@"```.*?```", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+\S+", RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"(?<!!)\[[^\]]+\]\([^)]+\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]+\)", RegexOptions.Compiled)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\b[\p{L}\p{N}][\p{L}\p{N}'-]*\b", RegexOptions.Compiled)]
    private static partial Regex WordRegex();
}
