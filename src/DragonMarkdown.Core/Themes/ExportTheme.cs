namespace DragonMarkdown.Core.Themes;

public sealed record ExportTheme(string Name)
{
    public static ExportTheme Default { get; } = new("Default");
}
