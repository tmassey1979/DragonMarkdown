namespace DragonMarkdown.Core.Exporting;

public sealed record MarkdownExportRequest(
    string Markdown,
    string SourcePath,
    string OutputPath,
    ExportFormat Format,
    ExportProfile? Profile = null)
{
    public ExportProfile EffectiveProfile => Profile ?? new ExportProfile(
        Format.ToString(),
        Format,
        new ExportPageSetup(),
        new ExportHeaderFooterOptions());
}
