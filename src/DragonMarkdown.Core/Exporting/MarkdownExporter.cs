using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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

    public void ExportToWord(string markdown, MarkdownRenderOptions options, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        EnsureOutputFolderExists(outputPath);

        var html = renderer.RenderDocument(markdown, options).Html;

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
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(options);
        EnsureOutputFolderExists(outputPath);

        QuestPDF.Settings.License = LicenseType.Community;
        var blocks = MarkdownPdfBlock.Parse(markdown);

        QuestDocument
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(42);
                    page.DefaultTextStyle(style => style.FontSize(11));

                    page.Content().Column(column =>
                    {
                        column.Spacing(8);

                        foreach (var block in blocks)
                        {
                            AddBlock(column, block);
                        }
                    });
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

            default:
                column.Item().Text(block.Text);
                break;
        }
    }

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

    private sealed record MarkdownPdfBlock(MarkdownPdfBlockKind Kind, string Text, int Level = 0)
    {
        public static IReadOnlyList<MarkdownPdfBlock> Parse(string markdown)
        {
            var blocks = new List<MarkdownPdfBlock>();
            var paragraph = new StringBuilder();
            var code = new StringBuilder();
            var inCodeFence = false;

            foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                var line = rawLine.TrimEnd();

                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraph);

                    if (inCodeFence)
                    {
                        blocks.Add(new MarkdownPdfBlock(MarkdownPdfBlockKind.Code, code.ToString().TrimEnd()));
                        code.Clear();
                        inCodeFence = false;
                    }
                    else
                    {
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
                blocks.Add(new MarkdownPdfBlock(MarkdownPdfBlockKind.Code, code.ToString().TrimEnd()));
            }

            return blocks.Count == 0
                ? [new MarkdownPdfBlock(MarkdownPdfBlockKind.Paragraph, string.Empty)]
                : blocks;
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
        Code
    }
}
