namespace DragonMarkdown.Core.FrontMatter;

public sealed record FrontMatterParseResult(
    IReadOnlyDictionary<string, string> Metadata,
    string Body);
