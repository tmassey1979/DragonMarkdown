namespace DragonMarkdown.Core.Exporting;

public sealed record BatchExportReport(
    string SourceFolder,
    string TargetFolder,
    IReadOnlyList<ExportResult> Results)
{
    public bool Succeeded => Results.All(result => result.Succeeded);
}
