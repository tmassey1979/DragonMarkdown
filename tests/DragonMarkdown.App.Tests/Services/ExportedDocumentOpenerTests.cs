using System.Diagnostics;
using DragonMarkdown.App.Services;

namespace DragonMarkdown.App.Tests.Services;

public sealed class ExportedDocumentOpenerTests : IDisposable
{
    private readonly string temporaryDirectory;

    public ExportedDocumentOpenerTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Opener.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void Open_ReturnsSuccessWhenProcessStartSucceeds()
    {
        var filePath = Path.Combine(temporaryDirectory, "notes.pdf");
        File.WriteAllText(filePath, "PDF");
        ProcessStartInfo? capturedInfo = null;
        var opener = new ExportedDocumentOpener(info =>
        {
            capturedInfo = info;
            return true;
        });

        var result = opener.Open(filePath);

        Assert.True(result.Succeeded);
        Assert.Equal(filePath, capturedInfo?.FileName);
        Assert.True(capturedInfo?.UseShellExecute);
    }

    [Fact]
    public void Open_ReturnsFailureWhenProcessStartThrows()
    {
        var filePath = Path.Combine(temporaryDirectory, "notes.pdf");
        File.WriteAllText(filePath, "PDF");
        var opener = new ExportedDocumentOpener(_ => throw new InvalidOperationException("No association"));

        var result = opener.Open(filePath);

        Assert.False(result.Succeeded);
        Assert.Contains("No association", result.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
