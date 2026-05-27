namespace DragonMarkdown.App.Services;

public interface IExportedDocumentOpener
{
    ExportedDocumentOpenResult Open(string filePath);
}

public sealed record ExportedDocumentOpenResult(
    bool Succeeded,
    string FilePath,
    string? ErrorMessage = null);
