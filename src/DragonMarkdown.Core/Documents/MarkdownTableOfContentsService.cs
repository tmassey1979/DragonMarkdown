using System.Text;
using System.Text.RegularExpressions;
using DragonMarkdown.Core.Rendering;

namespace DragonMarkdown.Core.Documents;

public sealed partial class MarkdownTableOfContentsService
{
    public const string StartMarker = "<!-- DragonMarkdown TOC -->";
    public const string EndMarker = "<!-- /DragonMarkdown TOC -->";

    private readonly MarkdownOutlineBuilder outlineBuilder = new();

    public string UpdateTableOfContents(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        string normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        var outline = outlineBuilder.Build(RemoveExistingTableOfContents(normalized))
            .Where(item => !item.Title.Equals("Table of Contents", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (outline.Length == 0)
        {
            return markdown;
        }

        string contentWithoutToc = RemoveExistingTableOfContents(normalized).TrimStart();
        string tableOfContents = BuildTableOfContents(outline);
        return tableOfContents + "\n\n" + contentWithoutToc;
    }

    private static string BuildTableOfContents(IReadOnlyList<MarkdownOutlineItem> outline)
    {
        var builder = new StringBuilder();
        builder.Append(StartMarker);
        builder.Append('\n');

        foreach (var item in outline)
        {
            int indentLevel = Math.Max(0, item.Level - 1);
            builder.Append(new string(' ', indentLevel * 2));
            builder.Append("- [");
            builder.Append(item.Title);
            builder.Append("](#");
            builder.Append(item.Slug);
            builder.Append(")\n");
        }

        builder.Append(EndMarker);
        return builder.ToString();
    }

    private static string RemoveExistingTableOfContents(string markdown) =>
        TableOfContentsBlockRegex().Replace(markdown, string.Empty);

    [GeneratedRegex(@"<!-- DragonMarkdown TOC -->.*?<!-- /DragonMarkdown TOC -->\s*", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex TableOfContentsBlockRegex();
}
