using DragonMarkdown.Core.Rendering;

namespace DragonMarkdown.Core.Exporting;

public sealed class BatchExportService
{
    private readonly MarkdownExporter exporter;

    public BatchExportService()
        : this(new MarkdownExporter())
    {
    }

    public BatchExportService(MarkdownExporter exporter)
    {
        this.exporter = exporter;
    }

    public BatchExportReport ExportFolder(string sourceFolder, string targetFolder, ExportProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFolder);
        ArgumentNullException.ThrowIfNull(profile);

        Directory.CreateDirectory(targetFolder);
        var results = new List<ExportResult>();

        foreach (var sourcePath in Directory.EnumerateFiles(sourceFolder, "*.md").Order(StringComparer.OrdinalIgnoreCase))
        {
            var outputPath = Path.Combine(
                targetFolder,
                Path.ChangeExtension(Path.GetFileName(sourcePath), GetExtension(profile.Format)));
            var markdown = File.ReadAllText(sourcePath);
            var request = new MarkdownExportRequest(markdown, sourcePath, outputPath, profile.Format, profile);
            var options = new MarkdownRenderOptions(Path.GetDirectoryName(sourcePath) ?? sourceFolder, sourcePath);

            results.Add(exporter.Export(request, options));
        }

        return new BatchExportReport(sourceFolder, targetFolder, results);
    }

    private static string GetExtension(ExportFormat format) =>
        format switch
        {
            ExportFormat.Word => ".docx",
            ExportFormat.Pdf => ".pdf",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
}
