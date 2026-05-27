namespace DragonMarkdown.Core.FrontMatter;

public static class FrontMatterService
{
    public static FrontMatterParseResult Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return new FrontMatterParseResult(new Dictionary<string, string>(), markdown);
        }

        var closingDelimiterIndex = normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (closingDelimiterIndex < 0)
        {
            return new FrontMatterParseResult(new Dictionary<string, string>(), markdown);
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var frontMatter = normalized[4..closingDelimiterIndex];

        foreach (var line in frontMatter.Split('\n'))
        {
            var separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            if (key.Length > 0)
            {
                metadata[key] = value;
            }
        }

        return new FrontMatterParseResult(
            metadata,
            normalized[(closingDelimiterIndex + "\n---\n".Length)..]);
    }
}
