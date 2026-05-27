using System.Diagnostics;

namespace DragonMarkdown.App.Services;

public sealed class ExportedDocumentOpener : IExportedDocumentOpener
{
    private readonly Func<ProcessStartInfo, bool> startProcess;

    public ExportedDocumentOpener()
        : this(info => Process.Start(info) is not null)
    {
    }

    public ExportedDocumentOpener(Func<ProcessStartInfo, bool> startProcess)
    {
        this.startProcess = startProcess;
    }

    public ExportedDocumentOpenResult Open(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new ExportedDocumentOpenResult(false, filePath, "A file path is required.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };

            return startProcess(startInfo)
                ? new ExportedDocumentOpenResult(true, filePath)
                : new ExportedDocumentOpenResult(false, filePath, "The operating system did not open the document.");
        }
        catch (Exception ex)
        {
            return new ExportedDocumentOpenResult(false, filePath, ex.Message);
        }
    }
}
