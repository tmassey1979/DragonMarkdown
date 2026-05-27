using System.Text;

namespace DragonMarkdown.Core.Rendering;

public sealed class MarkdownOutlineBuilder
{
    public IReadOnlyList<MarkdownOutlineItem> Build(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var outline = new List<MarkdownOutlineItem>();
        var usedSlugs = new Dictionary<string, int>(StringComparer.Ordinal);
        var insideCodeFence = false;

        using var reader = new StringReader(markdown);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                insideCodeFence = !insideCodeFence;
                continue;
            }

            if (insideCodeFence)
            {
                continue;
            }

            MarkdownOutlineItem? item = TryBuildHeading(trimmed, usedSlugs);
            if (item is not null)
            {
                outline.Add(item);
            }
        }

        return outline;
    }

    private static MarkdownOutlineItem? TryBuildHeading(string line, Dictionary<string, int> usedSlugs)
    {
        int level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        if (level is < 1 or > 6 || level >= line.Length || !char.IsWhiteSpace(line[level]))
        {
            return null;
        }

        string title = line[level..].Trim().TrimEnd('#').Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        return new MarkdownOutlineItem(level, title, GetUniqueSlug(CreateSlug(title), usedSlugs));
    }

    private static string CreateSlug(string title)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (char character in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (char.IsWhiteSpace(character) || character == '-')
            {
                if (!previousWasSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                    previousWasSeparator = true;
                }
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string GetUniqueSlug(string slug, Dictionary<string, int> usedSlugs)
    {
        string baseSlug = string.IsNullOrWhiteSpace(slug) ? "heading" : slug;
        if (!usedSlugs.TryGetValue(baseSlug, out int count))
        {
            usedSlugs[baseSlug] = 0;
            return baseSlug;
        }

        count++;
        usedSlugs[baseSlug] = count;
        return $"{baseSlug}-{count}";
    }
}
