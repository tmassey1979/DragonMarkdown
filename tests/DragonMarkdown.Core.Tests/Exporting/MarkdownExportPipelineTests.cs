using DragonMarkdown.Core.Exporting;
using DragonMarkdown.Core.Rendering;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Exporting;

public sealed class MarkdownExportPipelineTests : IDisposable
{
    private readonly string temporaryDirectory;

    public MarkdownExportPipelineTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Pipeline.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void Export_UsesRequestProfileAndReturnsResult()
    {
        var outputPath = Path.Combine(temporaryDirectory, "notes.pdf");
        var sourcePath = Path.Combine(temporaryDirectory, "notes.md");
        File.WriteAllText(sourcePath, "# Notes");
        var request = new MarkdownExportRequest(
            "# Notes",
            sourcePath,
            outputPath,
            ExportFormat.Pdf,
            ExportProfile.Pdf("PDF").WithPageSetup(new ExportPageSetup("Letter", 36)));

        var result = new MarkdownExporter().Export(request, new MarkdownRenderOptions(temporaryDirectory, sourcePath));

        result.Succeeded.Should().BeTrue();
        result.OutputPath.Should().Be(outputPath);
        result.ValidationReport.IsValid.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
