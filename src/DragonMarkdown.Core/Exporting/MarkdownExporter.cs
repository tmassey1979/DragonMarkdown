using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DragonMarkdown.Core.FrontMatter;
using DragonMarkdown.Core.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestDocument = QuestPDF.Fluent.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace DragonMarkdown.Core.Exporting;

public sealed class MarkdownExporter
{
    private readonly MarkdownRenderer renderer = new();
    private readonly ExportValidationService validationService = new();

    public ExportResult Export(MarkdownExportRequest request, MarkdownRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        var validationReport = validationService.Validate(request);
        if (!validationReport.IsValid)
        {
            return ExportResult.Failure(request.OutputPath, validationReport, "Export validation failed.");
        }

        try
        {
            var profile = request.EffectiveProfile;
            var markdown = profile.StripFrontMatter
                ? FrontMatterService.Parse(request.Markdown).Body
                : request.Markdown;

            switch (profile.Format)
            {
                case ExportFormat.Word:
                    ExportToWord(markdown, options, request.OutputPath, profile);
                    break;
                case ExportFormat.Pdf:
                    ExportToPdf(markdown, options, request.OutputPath, profile);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request), profile.Format, "Unsupported export format.");
            }

            return ExportResult.Success(request.OutputPath, validationReport);
        }
        catch (Exception ex)
        {
            return ExportResult.Failure(request.OutputPath, validationReport, ex.Message);
        }
    }

    public void ExportToWord(string markdown, MarkdownRenderOptions options, string outputPath)
        => ExportToWord(markdown, options, outputPath, ExportProfile.Word("Word"));

    public void ExportToWord(string markdown, MarkdownRenderOptions options, string outputPath, ExportProfile profile)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureOutputFolderExists(outputPath);

        var exportMarkdown = MarkdownExportPreprocessor.ReplaceMermaidFencesWithSvg(markdown);
        var html = renderer.RenderDocument(exportMarkdown, options).Html;

        using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new WordDocument(new Body());

        var htmlPart = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Html);
        using (var stream = htmlPart.GetStream(FileMode.Create, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
        {
            writer.Write(html);
        }

        var relationshipId = mainPart.GetIdOfPart(htmlPart);
        mainPart.Document.Body!.AppendChild(new AltChunk { Id = relationshipId });
        mainPart.Document.Save();
    }

    public void ExportToPdf(string markdown, MarkdownRenderOptions options, string outputPath)
        => ExportToPdf(markdown, options, outputPath, ExportProfile.Pdf("PDF"));

    public void ExportToPdf(string markdown, MarkdownRenderOptions options, string outputPath, ExportProfile profile)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureOutputFolderExists(outputPath);

        QuestPDF.Settings.License = LicenseType.Community;
        var blocks = MarkdownPdfBlock.Parse(markdown);

        QuestDocument
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(GetPageSize(profile.PageSetup.PageSize));
                    page.Margin(profile.PageSetup.MarginPoints);
                    page.DefaultTextStyle(style => style.FontSize(11));

                    if (profile.HeaderFooterOptions.Enabled
                        && !string.IsNullOrWhiteSpace(profile.HeaderFooterOptions.HeaderText))
                    {
                        page.Header().Text(profile.HeaderFooterOptions.HeaderText);
                    }

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        foreach (var block in blocks)
                        {
                            AddBlock(column, block);
                        }
                    });

                    if (profile.HeaderFooterOptions.Enabled
                        && !string.IsNullOrWhiteSpace(profile.HeaderFooterOptions.FooterText))
                    {
                        page.Footer().AlignCenter().Text(profile.HeaderFooterOptions.FooterText);
                    }
                });
            })
            .GeneratePdf(outputPath);
    }

    private static void AddBlock(ColumnDescriptor column, MarkdownPdfBlock block)
    {
        switch (block.Kind)
        {
            case MarkdownPdfBlockKind.Heading:
                column.Item()
                    .PaddingTop(block.Level == 1 ? 0 : 6)
                    .Text(block.Text)
                    .FontSize(block.Level switch
                    {
                        1 => 22,
                        2 => 18,
                        3 => 15,
                        _ => 13
                    })
                    .SemiBold();
                break;

            case MarkdownPdfBlockKind.Code:
                column.Item()
                    .Background(Colors.Grey.Lighten4)
                    .Padding(8)
                    .Text(block.Text)
                    .FontFamily("Consolas")
                    .FontSize(9);
                break;

            case MarkdownPdfBlockKind.MermaidDiagram:
                column.Item()
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .Svg(block.Svg!)
                    .FitWidth();
                break;

            default:
                column.Item().Text(block.Text);
                break;
        }
    }

    private static QuestPDF.Helpers.PageSize GetPageSize(string pageSize) =>
        pageSize.Trim().ToUpperInvariant() switch
        {
            "A4" => PageSizes.A4,
            "LETTER" => PageSizes.Letter,
            _ => PageSizes.Letter
        };

    private static void EnsureOutputFolderExists(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("An output path is required.", nameof(outputPath));
        }

        var folderPath = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    private sealed record MarkdownPdfBlock(MarkdownPdfBlockKind Kind, string Text, int Level = 0, string? Svg = null)
    {
        public static IReadOnlyList<MarkdownPdfBlock> Parse(string markdown)
        {
            var blocks = new List<MarkdownPdfBlock>();
            var paragraph = new StringBuilder();
            var code = new StringBuilder();
            var inCodeFence = false;
            var codeFenceLanguage = string.Empty;

            foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                var line = rawLine.TrimEnd();

                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraph);

                    if (inCodeFence)
                    {
                        AddCodeFence(blocks, codeFenceLanguage, code.ToString().TrimEnd());
                        code.Clear();
                        codeFenceLanguage = string.Empty;
                        inCodeFence = false;
                    }
                    else
                    {
                        codeFenceLanguage = line.TrimStart()[3..].Trim();
                        inCodeFence = true;
                    }

                    continue;
                }

                if (inCodeFence)
                {
                    code.AppendLine(rawLine);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph(blocks, paragraph);
                    continue;
                }

                var headingLevel = GetHeadingLevel(line);
                if (headingLevel > 0)
                {
                    FlushParagraph(blocks, paragraph);
                    blocks.Add(new MarkdownPdfBlock(
                        MarkdownPdfBlockKind.Heading,
                        line[(headingLevel + 1)..].Trim(),
                        headingLevel));
                    continue;
                }

                if (paragraph.Length > 0)
                {
                    paragraph.Append(' ');
                }

                paragraph.Append(CleanInlineMarkdown(line));
            }

            FlushParagraph(blocks, paragraph);

            if (inCodeFence && code.Length > 0)
            {
                AddCodeFence(blocks, codeFenceLanguage, code.ToString().TrimEnd());
            }

            return blocks.Count == 0
                ? [new MarkdownPdfBlock(MarkdownPdfBlockKind.Paragraph, string.Empty)]
                : blocks;
        }

        private static void AddCodeFence(ICollection<MarkdownPdfBlock> blocks, string language, string source)
        {
            if (string.Equals(language, "mermaid", StringComparison.OrdinalIgnoreCase)
                && MermaidDiagramRenderer.TryRender(source) is { } diagram)
            {
                blocks.Add(new MarkdownPdfBlock(MarkdownPdfBlockKind.MermaidDiagram, string.Empty, Svg: diagram.Svg));
                return;
            }

            blocks.Add(new MarkdownPdfBlock(MarkdownPdfBlockKind.Code, source));
        }

        private static void FlushParagraph(ICollection<MarkdownPdfBlock> blocks, StringBuilder paragraph)
        {
            if (paragraph.Length == 0)
            {
                return;
            }

            blocks.Add(new MarkdownPdfBlock(MarkdownPdfBlockKind.Paragraph, paragraph.ToString()));
            paragraph.Clear();
        }

        private static int GetHeadingLevel(string line)
        {
            var level = 0;
            while (level < line.Length && line[level] == '#')
            {
                level++;
            }

            return level is > 0 and <= 6 && level < line.Length && line[level] == ' '
                ? level
                : 0;
        }

        private static string CleanInlineMarkdown(string line) =>
            line.Replace("**", string.Empty, StringComparison.Ordinal)
                .Replace("__", string.Empty, StringComparison.Ordinal)
                .Replace("*", string.Empty, StringComparison.Ordinal)
                .Replace("`", string.Empty, StringComparison.Ordinal);
    }

    private enum MarkdownPdfBlockKind
    {
        Paragraph,
        Heading,
        Code,
        MermaidDiagram
    }
}
