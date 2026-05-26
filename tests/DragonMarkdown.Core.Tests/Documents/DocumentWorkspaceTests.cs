using DragonMarkdown.Core.Documents;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Documents;

public sealed class DocumentWorkspaceTests : IDisposable
{
    private readonly string temporaryDirectory;

    public DocumentWorkspaceTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void OpenDocumentLoadsFileStateAndMakesItActive()
    {
        string filePath = WriteMarkdownFile("README.md", "# DragonMarkdown");
        var workspace = new DocumentWorkspace();

        MarkdownDocument document = workspace.OpenDocument(filePath);

        document.FilePath.Should().Be(Path.GetFullPath(filePath));
        document.DisplayName.Should().Be("README.md");
        document.Text.Should().Be("# DragonMarkdown");
        document.OriginalText.Should().Be("# DragonMarkdown");
        document.IsDirty.Should().BeFalse();
        workspace.ActiveDocument.Should().BeSameAs(document);
        workspace.Documents.Should().ContainSingle().Which.Should().BeSameAs(document);
    }

    [Fact]
    public void UpdateTextTracksDirtyStateAgainstOriginalText()
    {
        string filePath = WriteMarkdownFile("notes.md", "Original notes");
        MarkdownDocument document = new DocumentWorkspace().OpenDocument(filePath);

        document.UpdateText("Edited notes");

        document.Text.Should().Be("Edited notes");
        document.OriginalText.Should().Be("Original notes");
        document.IsDirty.Should().BeTrue();

        document.UpdateText("Original notes");

        document.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void SaveWritesCurrentTextAndResetsOriginalText()
    {
        string filePath = WriteMarkdownFile("save.md", "Before save");
        MarkdownDocument document = new DocumentWorkspace().OpenDocument(filePath);
        document.UpdateText("After save");

        document.Save();

        File.ReadAllText(filePath).Should().Be("After save");
        document.OriginalText.Should().Be("After save");
        document.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void OpeningTheSameFileAgainReturnsExistingDocumentAndActivatesIt()
    {
        string firstPath = WriteMarkdownFile("first.md", "First");
        string secondPath = WriteMarkdownFile("second.md", "Second");
        var workspace = new DocumentWorkspace();
        MarkdownDocument firstDocument = workspace.OpenDocument(firstPath);
        MarkdownDocument secondDocument = workspace.OpenDocument(secondPath);

        MarkdownDocument reopenedDocument = workspace.OpenDocument(Path.GetFullPath(firstPath));

        reopenedDocument.Should().BeSameAs(firstDocument);
        reopenedDocument.Should().NotBeSameAs(secondDocument);
        workspace.Documents.Should().HaveCount(2);
        workspace.ActiveDocument.Should().BeSameAs(firstDocument);
    }

    [Fact]
    public void CloseDocumentClosesCleanDocumentsAndKeepsActiveDocumentValid()
    {
        string firstPath = WriteMarkdownFile("first.md", "First");
        string secondPath = WriteMarkdownFile("second.md", "Second");
        var workspace = new DocumentWorkspace();
        MarkdownDocument firstDocument = workspace.OpenDocument(firstPath);
        MarkdownDocument secondDocument = workspace.OpenDocument(secondPath);

        DocumentCloseResult secondCloseResult = workspace.CloseDocument(secondDocument);

        secondCloseResult.Should().Be(DocumentCloseResult.Closed);
        workspace.Documents.Should().ContainSingle().Which.Should().BeSameAs(firstDocument);
        workspace.ActiveDocument.Should().BeSameAs(firstDocument);

        DocumentCloseResult firstCloseResult = workspace.CloseDocument(firstDocument);

        firstCloseResult.Should().Be(DocumentCloseResult.Closed);
        workspace.Documents.Should().BeEmpty();
        workspace.ActiveDocument.Should().BeNull();
    }

    [Fact]
    public void CloseDocumentReturnsUnsavedDecisionAndLeavesDirtyDocumentOpen()
    {
        string filePath = WriteMarkdownFile("dirty.md", "Original");
        var workspace = new DocumentWorkspace();
        MarkdownDocument document = workspace.OpenDocument(filePath);
        document.UpdateText("Changed");

        DocumentCloseResult closeResult = workspace.CloseDocument(document);

        closeResult.Should().Be(DocumentCloseResult.UnsavedChangesNeedUserChoice);
        workspace.Documents.Should().ContainSingle().Which.Should().BeSameAs(document);
        workspace.ActiveDocument.Should().BeSameAs(document);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private string WriteMarkdownFile(string fileName, string text)
    {
        string filePath = Path.Combine(temporaryDirectory, fileName);
        File.WriteAllText(filePath, text);
        return filePath;
    }
}
