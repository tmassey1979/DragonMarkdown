using DragonMarkdown.Core.Exporting;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Exporting;

public sealed class BatchExportServiceTests : IDisposable
{
    private readonly string temporaryDirectory;

    public BatchExportServiceTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Batch.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void ExportFolder_ExportsMarkdownFilesToTargetFolder()
    {
        var sourceFolder = Path.Combine(temporaryDirectory, "source");
        var targetFolder = Path.Combine(temporaryDirectory, "target");
        Directory.CreateDirectory(sourceFolder);
        File.WriteAllText(Path.Combine(sourceFolder, "one.md"), "# One");
        File.WriteAllText(Path.Combine(sourceFolder, "two.md"), "# Two");
        File.WriteAllText(Path.Combine(sourceFolder, "ignored.txt"), "# Ignored");

        var profile = ExportProfile.Pdf("Batch PDF");

        var report = new BatchExportService().ExportFolder(sourceFolder, targetFolder, profile);

        report.Results.Should().HaveCount(2);
        report.Results.Should().OnlyContain(result => result.Succeeded);
        File.Exists(Path.Combine(targetFolder, "one.pdf")).Should().BeTrue();
        File.Exists(Path.Combine(targetFolder, "two.pdf")).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
