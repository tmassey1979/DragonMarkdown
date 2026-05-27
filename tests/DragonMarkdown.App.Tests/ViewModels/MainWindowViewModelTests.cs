using DragonMarkdown.App.ViewModels;

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

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
