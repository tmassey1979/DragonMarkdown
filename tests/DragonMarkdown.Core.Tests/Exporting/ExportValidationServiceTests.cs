using DragonMarkdown.Core.Exporting;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Exporting;

public sealed class ExportValidationServiceTests : IDisposable
{
    private readonly string temporaryDirectory;

    public ExportValidationServiceTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Validation.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void Validate_ReportsMissingLocalImages()
    {
        var request = new MarkdownExportRequest(
            """
            # Missing Image

            ![Diagram](images/missing.png)
            """,
            Path.Combine(temporaryDirectory, "notes.md"),
            Path.Combine(temporaryDirectory, "notes.pdf"),
            ExportFormat.Pdf);

        var report = new ExportValidationService().Validate(request);

        report.IsValid.Should().BeFalse();
        report.Errors.Should().Contain(issue => issue.Code == ExportValidationCodes.MissingLocalImage
            && issue.Reference.EndsWith(Path.Combine("images", "missing.png"), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WarnsAboutRawFileReferencesAndUnsupportedMermaid()
    {
        var sourcePath = Path.Combine(temporaryDirectory, "notes.md");
        var request = new MarkdownExportRequest(
            """
            [Local file](file:///C:/temp/secrets.txt)

            ```mermaid
            sequenceDiagram
                participant A
                participant B
                A->>B: Hello
            ```
            """,
            sourcePath,
            Path.Combine(temporaryDirectory, "notes.docx"),
            ExportFormat.Word);

        var report = new ExportValidationService().Validate(request);

        report.IsValid.Should().BeTrue();
        report.Warnings.Should().Contain(issue => issue.Code == ExportValidationCodes.RawFileReference);
        report.Warnings.Should().Contain(issue => issue.Code == ExportValidationCodes.UnsupportedMermaid);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
