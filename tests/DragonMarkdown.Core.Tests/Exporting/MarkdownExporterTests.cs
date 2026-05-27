using System.IO.Compression;
using System.Text;
using DragonMarkdown.Core.Exporting;
using DragonMarkdown.Core.Rendering;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Exporting;

public sealed class MarkdownExporterTests : IDisposable
{
    private readonly string temporaryDirectory;

    public MarkdownExporterTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Export.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void ExportToWord_CreatesWordDocumentWithRenderedHtml()
    {
        var outputPath = Path.Combine(temporaryDirectory, "notes.docx");
        var exporter = new MarkdownExporter();

        exporter.ExportToWord(
            """
            # Release Notes

            | Item | State |
            | --- | --- |
            | Export | Done |
            """,
            CreateOptions("notes.md"),
            outputPath);

        File.Exists(outputPath).Should().BeTrue();
        using var archive = ZipFile.OpenRead(outputPath);
        archive.Entries.Should().Contain(entry => entry.FullName == "word/document.xml");
        archive.Entries.Should().Contain(entry => entry.FullName.StartsWith("word/afchunk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExportToWord_RendersMermaidFlowchartsAsSvgInHtmlChunk()
    {
        var outputPath = Path.Combine(temporaryDirectory, "diagram.docx");
        var exporter = new MarkdownExporter();

        exporter.ExportToWord(
            """
            # Diagram

            ```mermaid
            graph TD
                A[Draft] --> B[Preview]
                B --> C[Export]
            ```
            """,
            CreateOptions("diagram.md"),
            outputPath);

        using var archive = ZipFile.OpenRead(outputPath);
        var htmlEntry = archive.Entries.Single(entry => entry.FullName.StartsWith("word/afchunk", StringComparison.OrdinalIgnoreCase));
        using var stream = htmlEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var html = reader.ReadToEnd();

        html.Should().Contain("<svg");
        html.Should().Contain("dragon-mermaid-diagram");
        html.Should().Contain("Draft");
        html.Should().Contain("Preview");
        html.Should().Contain("Export");
    }

    [Fact]
    public void ExportToPdf_CreatesPdfDocument()
    {
        var outputPath = Path.Combine(temporaryDirectory, "notes.pdf");
        var exporter = new MarkdownExporter();

        exporter.ExportToPdf(
            """
            # Release Notes

            Export to PDF from the active markdown document.
            """,
            CreateOptions("notes.md"),
            outputPath);

        var bytes = File.ReadAllBytes(outputPath);
        Encoding.ASCII.GetString(bytes[..5]).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(1_000);
    }

    [Fact]
    public void ExportToPdf_CreatesPdfDocumentWithMermaidDiagram()
    {
        var outputPath = Path.Combine(temporaryDirectory, "diagram.pdf");
        var exporter = new MarkdownExporter();

        exporter.ExportToPdf(
            """
            # Diagram

            ```mermaid
            flowchart LR
                A[Draft] --> B[Preview]
                B --> C[Export]
            ```
            """,
            CreateOptions("diagram.md"),
            outputPath);

        var bytes = File.ReadAllBytes(outputPath);
        Encoding.ASCII.GetString(bytes[..5]).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(1_000);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private MarkdownRenderOptions CreateOptions(string documentName)
    {
        var documentPath = Path.Combine(temporaryDirectory, documentName);
        File.WriteAllText(documentPath, string.Empty);
        return new MarkdownRenderOptions(temporaryDirectory, documentPath);
    }
}
