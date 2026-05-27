namespace DragonMarkdown.Core.Exporting;

public sealed record ExportProfile(
    string Name,
    ExportFormat Format,
    ExportPageSetup PageSetup,
    ExportHeaderFooterOptions HeaderFooterOptions,
    bool StripFrontMatter = true)
{
    public static ExportProfile Word(string name) =>
        new(name, ExportFormat.Word, new ExportPageSetup(), new ExportHeaderFooterOptions());

    public static ExportProfile Pdf(string name) =>
        new(name, ExportFormat.Pdf, new ExportPageSetup(), new ExportHeaderFooterOptions());

    public ExportProfile WithPageSetup(ExportPageSetup pageSetup) => this with { PageSetup = pageSetup };

    public ExportProfile WithHeaderFooter(ExportHeaderFooterOptions headerFooterOptions) =>
        this with { HeaderFooterOptions = headerFooterOptions };
}
