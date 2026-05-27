using DragonMarkdown.App.ViewModels;
using DragonMarkdown.App.Services;

namespace DragonMarkdown.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests : IDisposable
{
    private readonly string temporaryDirectory;

    public MainWindowViewModelTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.App.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
    }

    [Fact]
    public void OpenPath_WithFolder_LoadsWorkspaceTree()
    {
        Directory.CreateDirectory(Path.Combine(temporaryDirectory, "docs"));
        File.WriteAllText(Path.Combine(temporaryDirectory, "README.md"), "# DragonMarkdown");
        var viewModel = new MainWindowViewModel();

        viewModel.OpenPath(temporaryDirectory);

        Assert.Equal(Path.GetFullPath(temporaryDirectory), viewModel.WorkspaceRootPath);
        Assert.Equal(Path.GetFullPath(temporaryDirectory), viewModel.WorkspaceLabel);
        Assert.NotEmpty(viewModel.WorkspaceItems);
        Assert.Empty(viewModel.OpenDocuments);
    }

    [Fact]
    public void OpenPath_WithMarkdownFile_LoadsContainingFolderAndDocument()
    {
        var filePath = Path.Combine(temporaryDirectory, "README.md");
        File.WriteAllText(filePath, "# DragonMarkdown");
        var viewModel = new MainWindowViewModel();

        viewModel.OpenPath(filePath);

        Assert.Equal(Path.GetFullPath(temporaryDirectory), viewModel.WorkspaceRootPath);
        Assert.Single(viewModel.OpenDocuments);
        Assert.Equal("README.md", viewModel.SelectedDocument?.DisplayName);
    }

    [Fact]
    public void OpenPath_WithMissingPath_ReportsMissingPath()
    {
        var missingPath = Path.Combine(temporaryDirectory, "missing.md");
        var viewModel = new MainWindowViewModel();

        viewModel.OpenPath(missingPath);

        Assert.Equal($"Path not found: {Path.GetFullPath(missingPath)}", viewModel.StatusText);
    }

    [Fact]
    public void SelectingMarkdownWorkspaceItem_OpensDocumentAndRefreshesPreview()
    {
        var filePath = Path.Combine(temporaryDirectory, "README.md");
        File.WriteAllText(filePath, "# Preview Me");
        var viewModel = new MainWindowViewModel();
        string? previewHtml = null;
        viewModel.PreviewHtmlChanged += (_, html) => previewHtml = html;

        viewModel.SelectedWorkspaceItem = new WorkspaceNodeViewModel(
            "README.md",
            filePath,
            "README.md",
            WorkspaceNodeKind.Markdown);

        Assert.Single(viewModel.OpenDocuments);
        Assert.Equal("README.md", viewModel.SelectedDocument?.DisplayName);
        Assert.Contains("<h1", previewHtml, StringComparison.Ordinal);
        Assert.Contains("Preview Me", previewHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectingAssetWorkspaceItem_DoesNotOpenDocument()
    {
        var filePath = Path.Combine(temporaryDirectory, "logo.png");
        File.WriteAllText(filePath, "asset");
        var viewModel = new MainWindowViewModel();

        viewModel.SelectedWorkspaceItem = new WorkspaceNodeViewModel(
            "logo.png",
            filePath,
            "logo.png",
            WorkspaceNodeKind.Asset);

        Assert.Empty(viewModel.OpenDocuments);
        Assert.Null(viewModel.SelectedDocument);
    }

    [Fact]
    public void HidingPreview_ExpandsEditorAcrossDocumentWorkArea()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.TogglePreviewCommand.Execute(null);

        Assert.True(viewModel.IsEditorVisible);
        Assert.False(viewModel.IsPreviewVisible);
        Assert.Equal(3, viewModel.EditorColumnSpan);
        Assert.False(viewModel.MiddleSplitterVisible);
    }

    [Fact]
    public void HidingEditor_ExpandsPreviewAcrossDocumentWorkArea()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.ToggleEditorCommand.Execute(null);

        Assert.False(viewModel.IsEditorVisible);
        Assert.True(viewModel.IsPreviewVisible);
        Assert.Equal(2, viewModel.PreviewGridColumn);
        Assert.Equal(3, viewModel.PreviewColumnSpan);
        Assert.False(viewModel.MiddleSplitterVisible);
    }

    [Fact]
    public void ToggleCommands_DoNotHideBothDocumentPanes()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.TogglePreviewCommand.Execute(null);
        viewModel.ToggleEditorCommand.Execute(null);

        Assert.True(viewModel.IsEditorVisible);
        Assert.False(viewModel.IsPreviewVisible);
        Assert.Equal("Keep either the editor or preview visible.", viewModel.StatusText);
    }

    [Fact]
    public void OpenCommands_RaiseDialogRequestEvents()
    {
        var viewModel = new MainWindowViewModel();
        var folderRequests = 0;
        var fileRequests = 0;
        viewModel.OpenFolderRequested += (_, _) => folderRequests++;
        viewModel.OpenFileRequested += (_, _) => fileRequests++;

        viewModel.OpenFolderCommand.Execute(null);
        viewModel.OpenFileCommand.Execute(null);

        Assert.Equal(1, folderRequests);
        Assert.Equal(1, fileRequests);
    }

    [Fact]
    public void ExportCommands_RequireActiveDocument()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.ExportWordCommand.Execute(null);
        Assert.Equal("No active document to export.", viewModel.StatusText);

        viewModel.ExportPdfCommand.Execute(null);
        Assert.Equal("No active document to export.", viewModel.StatusText);
    }

    [Fact]
    public void ExportCommands_RaiseSuggestedFileNames()
    {
        var filePath = Path.Combine(temporaryDirectory, "release-notes.md");
        File.WriteAllText(filePath, "# Release Notes");
        var viewModel = new MainWindowViewModel();
        string? wordSuggestion = null;
        string? pdfSuggestion = null;
        viewModel.OpenFile(filePath);
        viewModel.ExportWordRequested += (_, fileName) => wordSuggestion = fileName;
        viewModel.ExportPdfRequested += (_, fileName) => pdfSuggestion = fileName;

        viewModel.ExportWordCommand.Execute(null);
        viewModel.ExportPdfCommand.Execute(null);

        Assert.Equal("release-notes.docx", wordSuggestion);
        Assert.Equal("release-notes.pdf", pdfSuggestion);
    }

    [Fact]
    public void ExportMethods_WriteFilesAndUpdateStatus()
    {
        var filePath = Path.Combine(temporaryDirectory, "export.md");
        var wordPath = Path.Combine(temporaryDirectory, "export.docx");
        var pdfPath = Path.Combine(temporaryDirectory, "export.pdf");
        File.WriteAllText(filePath, "# Export");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFile(filePath);

        viewModel.ExportActiveDocumentToWord(wordPath);
        Assert.True(File.Exists(wordPath));
        Assert.Equal("Exported Word document export.docx", viewModel.StatusText);

        viewModel.ExportActiveDocumentToPdf(pdfPath);
        Assert.True(File.Exists(pdfPath));
        Assert.Equal("Exported PDF export.pdf", viewModel.StatusText);
    }

    [Fact]
    public void ExportMethods_OpenGeneratedDocumentWhenOpenerIsConfigured()
    {
        var filePath = Path.Combine(temporaryDirectory, "export-open.md");
        var wordPath = Path.Combine(temporaryDirectory, "export-open.docx");
        File.WriteAllText(filePath, "# Export And Open");
        var opener = new RecordingExportedDocumentOpener();
        var viewModel = new MainWindowViewModel(exportedDocumentOpener: opener);
        viewModel.OpenFile(filePath);

        viewModel.ExportActiveDocumentToWord(wordPath);

        Assert.Equal(wordPath, opener.OpenedPath);
        Assert.Equal("Exported Word document export-open.docx and opened it.", viewModel.StatusText);
    }

    [Fact]
    public void SaveCommands_HandleNoActiveDocumentAndDirtyDocuments()
    {
        var filePath = Path.Combine(temporaryDirectory, "notes.md");
        File.WriteAllText(filePath, "Before");
        var viewModel = new MainWindowViewModel();

        viewModel.SaveActiveCommand.Execute(null);
        Assert.Equal("No active document to save.", viewModel.StatusText);

        viewModel.OpenFile(filePath);
        viewModel.SelectedDocument!.Text = "After";

        viewModel.SaveActiveCommand.Execute(null);
        Assert.Equal("After", File.ReadAllText(filePath));
        Assert.False(viewModel.SelectedDocument.IsDirty);

        viewModel.SelectedDocument.Text = "After all";
        viewModel.SaveAllCommand.Execute(null);

        Assert.Equal("After all", File.ReadAllText(filePath));
        Assert.Equal("Saved all open files", viewModel.StatusText);
    }

    [Fact]
    public void CreateCommands_RequireWorkspaceAndCreateItems()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.CreateFileCommand.Execute(null);
        Assert.Equal("Open a folder before creating files.", viewModel.StatusText);

        viewModel.CreateFolderCommand.Execute(null);
        Assert.Equal("Open a folder before creating folders.", viewModel.StatusText);

        viewModel.OpenFolder(temporaryDirectory);
        viewModel.CreateFileCommand.Execute(null);
        viewModel.CreateFolderCommand.Execute(null);

        Assert.True(File.Exists(Path.Combine(temporaryDirectory, "Untitled.md")));
        Assert.True(Directory.Exists(Path.Combine(temporaryDirectory, "New Folder")));
        Assert.Contains(viewModel.WorkspaceItems, item => item.Name == "Untitled.md");
    }

    [Fact]
    public void CreateFile_UsesSelectedFolderAsTargetAndUniqueNames()
    {
        var folderPath = Path.Combine(temporaryDirectory, "docs");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "Untitled.md"), "# Existing");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFolder(temporaryDirectory);
        viewModel.SelectedWorkspaceItem = new WorkspaceNodeViewModel("docs", folderPath, "docs", WorkspaceNodeKind.Folder);

        viewModel.CreateFileCommand.Execute(null);

        Assert.True(File.Exists(Path.Combine(folderPath, "Untitled 2.md")));
    }

    [Fact]
    public void DeleteSelected_DeletesFilesAndFolders()
    {
        var filePath = Path.Combine(temporaryDirectory, "delete.md");
        var folderPath = Path.Combine(temporaryDirectory, "delete-folder");
        File.WriteAllText(filePath, "# Delete");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "child.md"), "# Child");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFolder(temporaryDirectory);

        viewModel.DeleteSelectedCommand.Execute(null);
        Assert.Equal("Select a file or folder to delete.", viewModel.StatusText);

        viewModel.SelectedWorkspaceItem = new WorkspaceNodeViewModel("delete.md", filePath, "delete.md", WorkspaceNodeKind.Markdown);
        viewModel.DeleteSelectedCommand.Execute(null);
        Assert.False(File.Exists(filePath));

        viewModel.SelectedWorkspaceItem = new WorkspaceNodeViewModel("delete-folder", folderPath, "delete-folder", WorkspaceNodeKind.Folder);
        viewModel.DeleteSelectedCommand.Execute(null);
        Assert.False(Directory.Exists(folderPath));
    }

    [Fact]
    public void RevealSelected_RequiresSelection()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.RevealSelectedCommand.Execute(null);

        Assert.Equal("Select a file or folder to reveal.", viewModel.StatusText);
    }

    [Fact]
    public void EditingSelectedDocument_TracksDirtyStateAndRefreshesPreview()
    {
        var filePath = Path.Combine(temporaryDirectory, "edit.md");
        File.WriteAllText(filePath, "# Before");
        var viewModel = new MainWindowViewModel();
        var previewEvents = new List<string>();
        viewModel.PreviewHtmlChanged += (_, html) => previewEvents.Add(html);
        viewModel.OpenFile(filePath);

        viewModel.SelectedDocument!.Text = "# After";

        Assert.True(viewModel.SelectedDocument.IsDirty);
        Assert.Contains(previewEvents, html => html.Contains("After", StringComparison.Ordinal));
    }

    [Fact]
    public void OpenHelpCommand_OpensConfiguredHelpDocument()
    {
        var helpPath = Path.Combine(temporaryDirectory, "DragonMarkdownHelp.md");
        File.WriteAllText(helpPath, "# DragonMarkdown Help");
        var viewModel = new MainWindowViewModel(helpDocumentPath: helpPath);

        viewModel.OpenHelpCommand.Execute(null);

        Assert.Single(viewModel.OpenDocuments);
        Assert.Equal("DragonMarkdownHelp.md", viewModel.SelectedDocument?.DisplayName);
    }

    [Fact]
    public void ShowAboutCommand_RaisesAboutRequest()
    {
        var viewModel = new MainWindowViewModel();
        var requestCount = 0;
        viewModel.AboutRequested += (_, _) => requestCount++;

        viewModel.ShowAboutCommand.Execute(null);

        Assert.Equal(1, requestCount);
    }

    [Fact]
    public void OpenHelpCommand_ReportsMissingHelpDocument()
    {
        var helpPath = Path.Combine(temporaryDirectory, "missing-help.md");
        var viewModel = new MainWindowViewModel(helpDocumentPath: helpPath);

        viewModel.OpenHelpCommand.Execute(null);

        Assert.Equal("Help document not found.", viewModel.StatusText);
    }

    [Fact]
    public void WorkspaceNodeViewModel_MapsWorkspaceItemsAndGlyphs()
    {
        var workspaceItem = new DragonMarkdown.Core.Workspaces.WorkspaceItem(
            "docs",
            Path.Combine(temporaryDirectory, "docs"),
            "docs",
            DragonMarkdown.Core.Workspaces.WorkspaceItemKind.Folder,
            [
                new(
                    "README.md",
                    Path.Combine(temporaryDirectory, "docs", "README.md"),
                    "docs/README.md",
                    DragonMarkdown.Core.Workspaces.WorkspaceItemKind.MarkdownFile),
                new(
                    "logo.svg",
                    Path.Combine(temporaryDirectory, "docs", "logo.svg"),
                    "docs/logo.svg",
                    DragonMarkdown.Core.Workspaces.WorkspaceItemKind.AssetFile)
            ]);

        var node = WorkspaceNodeViewModel.FromWorkspaceItem(workspaceItem);

        Assert.Equal(WorkspaceNodeKind.Folder, node.Kind);
        Assert.Equal("DIR", node.Glyph);
        Assert.Equal("MD", node.Children[0].Glyph);
        Assert.Equal("FILE", node.Children[1].Glyph);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private sealed class RecordingExportedDocumentOpener : IExportedDocumentOpener
    {
        public string? OpenedPath { get; private set; }

        public ExportedDocumentOpenResult Open(string filePath)
        {
            OpenedPath = filePath;
            return new ExportedDocumentOpenResult(true, filePath);
        }
    }
}
