using DragonMarkdown.App.ViewModels;
using DragonMarkdown.App.Services;
using DragonMarkdown.Core.Exporting;
using DragonMarkdown.Core.Health;
using DragonMarkdown.Core.Workspaces;

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
        var recentItemsService = new RecordingRecentItemsService();
        var viewModel = new MainWindowViewModel(recentItemsService: recentItemsService);

        viewModel.OpenPath(temporaryDirectory);

        Assert.Equal(Path.GetFullPath(temporaryDirectory), viewModel.WorkspaceRootPath);
        Assert.Equal(Path.GetFullPath(temporaryDirectory), viewModel.WorkspaceLabel);
        Assert.NotEmpty(viewModel.WorkspaceItems);
        Assert.Empty(viewModel.OpenDocuments);
        Assert.Contains(Path.GetFullPath(temporaryDirectory), recentItemsService.Paths);
    }

    [Fact]
    public void OpenPath_WithMarkdownFile_LoadsContainingFolderAndDocument()
    {
        var filePath = Path.Combine(temporaryDirectory, "README.md");
        File.WriteAllText(filePath, "# DragonMarkdown");
        var recentItemsService = new RecordingRecentItemsService();
        var viewModel = new MainWindowViewModel(recentItemsService: recentItemsService);

        viewModel.OpenPath(filePath);

        Assert.Equal(Path.GetFullPath(temporaryDirectory), viewModel.WorkspaceRootPath);
        Assert.Single(viewModel.OpenDocuments);
        Assert.Equal("README.md", viewModel.SelectedDocument?.DisplayName);
        Assert.Contains(Path.GetFullPath(filePath), recentItemsService.Paths);
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
    public void SettingsCommands_OpenAndPersistSettings()
    {
        var settingsService = new RecordingUserSettingsService(new UserSettings(temporaryDirectory, "Dark", false));
        var viewModel = new MainWindowViewModel(userSettingsService: settingsService);

        viewModel.OpenSettingsCommand.Execute(null);
        viewModel.SettingsTheme = "Light";
        viewModel.SettingsWordWrap = true;
        viewModel.SaveSettingsCommand.Execute(null);

        Assert.False(viewModel.IsSettingsOpen);
        Assert.Equal(new UserSettings(null, "Light", true), settingsService.SavedSettings);
        Assert.Equal("Settings saved.", viewModel.StatusText);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_ReportsAvailableUpdate()
    {
        var updateService = new RecordingUpdateCheckService(
            new UpdateCheckResult(true, "v9.9.9", new Uri("https://example.test/release"), "v9.9.9 is available."));
        var viewModel = new MainWindowViewModel(updateCheckService: updateService);

        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.Equal("v9.9.9 is available.", viewModel.StatusText);
        Assert.True(updateService.Checked);
    }

    [Fact]
    public void OpenWorkspaceSearchResultCommand_OpensMatchingDocument()
    {
        var filePath = Path.Combine(temporaryDirectory, "search.md");
        File.WriteAllText(filePath, "# Search Hit");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFolder(temporaryDirectory);
        viewModel.WorkspaceSearchText = "Search";
        var result = Assert.Single(viewModel.WorkspaceSearchResults);

        viewModel.OpenWorkspaceSearchResultCommand.Execute(result);

        Assert.Equal("search.md", viewModel.SelectedDocument?.DisplayName);
    }

    [Fact]
    public void RefreshWorkspaceHealthCommand_RequiresOpenWorkspace()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.RefreshWorkspaceHealthCommand.Execute(null);

        Assert.Equal("Open a folder before analyzing docs health.", viewModel.StatusText);
        Assert.Equal("Docs health not analyzed", viewModel.WorkspaceHealthSummary);
        Assert.Empty(viewModel.WorkspaceHealthIssues);
    }

    [Fact]
    public void OpenFolder_AnalyzesWorkspaceHealth()
    {
        File.WriteAllText(Path.Combine(temporaryDirectory, "README.md"), "# Home" + Environment.NewLine + "[Missing](missing.md)");
        var viewModel = new MainWindowViewModel();

        viewModel.OpenFolder(temporaryDirectory);

        var issue = Assert.Single(viewModel.WorkspaceHealthIssues);
        Assert.Equal(WorkspaceHealthIssueCodes.BrokenLink, issue.Code);
        Assert.Equal("README.md", issue.RelativePath);
        Assert.Equal("Docs health: 1 error, 0 warnings, 0 notes", viewModel.WorkspaceHealthSummary);
    }

    [Fact]
    public void OpenWorkspaceHealthIssueCommand_OpensIssueDocument()
    {
        var readmePath = Path.Combine(temporaryDirectory, "README.md");
        File.WriteAllText(readmePath, "# Home" + Environment.NewLine + "![Missing](missing.png)");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFolder(temporaryDirectory);
        var issue = Assert.Single(viewModel.WorkspaceHealthIssues);

        viewModel.OpenWorkspaceHealthIssueCommand.Execute(issue);

        Assert.Equal("README.md", viewModel.SelectedDocument?.DisplayName);
        Assert.Equal("Opened README.md", viewModel.StatusText);
    }

    [Fact]
    public void OpenRecentItemCommand_OpensRecentPath()
    {
        var filePath = Path.Combine(temporaryDirectory, "recent.md");
        File.WriteAllText(filePath, "# Recent");
        var viewModel = new MainWindowViewModel();
        var recentItem = new RecentItem(filePath, DateTimeOffset.UtcNow);

        viewModel.OpenRecentItemCommand.Execute(recentItem);

        Assert.Equal("recent.md", viewModel.SelectedDocument?.DisplayName);
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
    public void ValidateExportReadinessCommand_ReportsActiveDocumentIssues()
    {
        var filePath = Path.Combine(temporaryDirectory, "readiness.md");
        File.WriteAllText(filePath, "# Readiness" + Environment.NewLine + "![Missing](missing.png)");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFile(filePath);

        viewModel.ValidateExportReadinessCommand.Execute(null);

        var issue = Assert.Single(viewModel.ExportValidationIssues);
        Assert.Equal(ExportValidationCodes.MissingLocalImage, issue.Code);
        Assert.Equal("Error", issue.Severity);
        Assert.Equal("Export readiness: 1 error, 0 warnings", viewModel.ExportValidationSummary);
    }

    [Fact]
    public void ExportMethods_StopWhenValidationHasErrors()
    {
        var filePath = Path.Combine(temporaryDirectory, "blocked-export.md");
        var pdfPath = Path.Combine(temporaryDirectory, "blocked-export.pdf");
        File.WriteAllText(filePath, "# Blocked" + Environment.NewLine + "![Missing](missing.png)");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFile(filePath);

        viewModel.ExportActiveDocumentToPdf(pdfPath);

        Assert.False(File.Exists(pdfPath));
        Assert.Equal("Export validation failed: 1 error, 0 warnings", viewModel.StatusText);
        Assert.Single(viewModel.ExportValidationIssues);
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
    public async Task EditingSelectedDocument_DebouncesPreviewRefresh()
    {
        var filePath = Path.Combine(temporaryDirectory, "edit.md");
        File.WriteAllText(filePath, "# Before");
        var scheduler = new ManualPreviewRefreshScheduler();
        var viewModel = new MainWindowViewModel(previewRefreshScheduler: scheduler);
        var previewEvents = new List<string>();
        viewModel.PreviewHtmlChanged += (_, html) => previewEvents.Add(html);
        viewModel.OpenFile(filePath);
        previewEvents.Clear();

        viewModel.SelectedDocument!.Text = "# After";
        viewModel.SelectedDocument.Text = "# Final";

        Assert.True(viewModel.SelectedDocument.IsDirty);
        Assert.Equal(2, scheduler.ScheduledCount);
        Assert.Empty(previewEvents);

        await scheduler.RunLatestAsync();

        Assert.True(viewModel.SelectedDocument.IsDirty);
        Assert.Single(previewEvents);
        Assert.Contains("Final", previewEvents[0], StringComparison.Ordinal);
        Assert.DoesNotContain("After", previewEvents[0], StringComparison.Ordinal);
    }

    [Fact]
    public void EditingSelectedDocument_WritesAutosaveSnapshot()
    {
        var filePath = Path.Combine(temporaryDirectory, "autosave.md");
        File.WriteAllText(filePath, "# Before");
        var autosaveService = new RecordingAutosaveRecoveryService();
        var viewModel = new MainWindowViewModel(autosaveRecoveryService: autosaveService);
        viewModel.OpenFile(filePath);

        viewModel.SelectedDocument!.Text = "# Draft";

        Assert.Equal(Path.GetFullPath(filePath), autosaveService.SnapshotDocumentPath);
        Assert.Equal("# Draft", autosaveService.SnapshotContent);
    }

    [Fact]
    public void SaveActive_ClearsAutosaveSnapshot()
    {
        var filePath = Path.Combine(temporaryDirectory, "autosave-save.md");
        File.WriteAllText(filePath, "# Before");
        var autosaveService = new RecordingAutosaveRecoveryService();
        var viewModel = new MainWindowViewModel(autosaveRecoveryService: autosaveService);
        viewModel.OpenFile(filePath);
        viewModel.SelectedDocument!.Text = "# Draft";

        viewModel.SaveActiveCommand.Execute(null);

        Assert.Equal(Path.GetFullPath(filePath), autosaveService.ClearedDocumentPath);
    }

    [Fact]
    public void OpenFile_RefreshesPreviewImmediately()
    {
        var filePath = Path.Combine(temporaryDirectory, "open-preview.md");
        File.WriteAllText(filePath, "# Immediate");
        var scheduler = new ManualPreviewRefreshScheduler();
        var viewModel = new MainWindowViewModel(previewRefreshScheduler: scheduler);
        string? previewHtml = null;
        viewModel.PreviewHtmlChanged += (_, html) => previewHtml = html;

        viewModel.OpenFile(filePath);

        Assert.Equal(0, scheduler.ScheduledCount);
        Assert.Contains("Immediate", previewHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void OpeningDocument_BuildsDocumentOutline()
    {
        var filePath = Path.Combine(temporaryDirectory, "outline.md");
        File.WriteAllText(filePath, "# Heading 1" + Environment.NewLine + "## Heading 2");
        var viewModel = new MainWindowViewModel();

        viewModel.OpenFile(filePath);

        Assert.Collection(
            viewModel.DocumentOutline,
            item => Assert.Equal("Heading 1", item.Title),
            item => Assert.Equal("Heading 2", item.Title));
    }

    [Fact]
    public void OpeningDocument_UpdatesActiveDocumentStatistics()
    {
        var filePath = Path.Combine(temporaryDirectory, "stats.md");
        File.WriteAllText(filePath, "# Stats" + Environment.NewLine + "DragonMarkdown tracks useful writing data.");
        var viewModel = new MainWindowViewModel();

        viewModel.OpenFile(filePath);

        Assert.Equal("6 words | 1 heading | 0 links | 0 images | 1 min read", viewModel.ActiveDocumentStatisticsSummary);
    }

    [Fact]
    public void OpeningDocument_LoadsBacklinksFromWorkspace()
    {
        Directory.CreateDirectory(Path.Combine(temporaryDirectory, "docs"));
        var targetPath = Path.Combine(temporaryDirectory, "docs", "guide.md");
        File.WriteAllText(Path.Combine(temporaryDirectory, "README.md"), "# Home" + Environment.NewLine + "[Guide](docs/guide.md)");
        File.WriteAllText(targetPath, "# Guide");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFolder(temporaryDirectory);

        viewModel.OpenFile(targetPath);

        var backlink = Assert.Single(viewModel.DocumentBacklinks);
        Assert.Equal("Home", backlink.Title);
        Assert.Equal("README.md", backlink.RelativePath);
        Assert.Equal("1 backlink", viewModel.DocumentBacklinksSummary);
    }

    [Fact]
    public void OpenDocumentBacklinkCommand_OpensReferringDocument()
    {
        Directory.CreateDirectory(Path.Combine(temporaryDirectory, "docs"));
        var targetPath = Path.Combine(temporaryDirectory, "docs", "guide.md");
        File.WriteAllText(Path.Combine(temporaryDirectory, "README.md"), "# Home" + Environment.NewLine + "[Guide](docs/guide.md)");
        File.WriteAllText(targetPath, "# Guide");
        var viewModel = new MainWindowViewModel();
        viewModel.OpenFolder(temporaryDirectory);
        viewModel.OpenFile(targetPath);

        viewModel.OpenDocumentBacklinkCommand.Execute(viewModel.DocumentBacklinks.Single());

        Assert.Equal("README.md", viewModel.SelectedDocument?.DisplayName);
    }

    [Fact]
    public async Task EditingDocument_UpdatesActiveDocumentStatisticsAfterPreviewRefresh()
    {
        var filePath = Path.Combine(temporaryDirectory, "stats-edit.md");
        File.WriteAllText(filePath, "# Before");
        var scheduler = new ManualPreviewRefreshScheduler();
        var viewModel = new MainWindowViewModel(previewRefreshScheduler: scheduler);
        viewModel.OpenFile(filePath);

        viewModel.SelectedDocument!.Text = "# After" + Environment.NewLine + "[Link](next.md) ![Image](logo.png)";
        await scheduler.RunLatestAsync();

        Assert.Equal("1 word | 1 heading | 1 link | 1 image | 1 min read", viewModel.ActiveDocumentStatisticsSummary);
    }

    [Fact]
    public void SelectingOutlineItem_RaisesPreviewAnchorRequestAndUpdatesStatus()
    {
        var filePath = Path.Combine(temporaryDirectory, "outline-anchor.md");
        File.WriteAllText(filePath, "# Heading 1");
        var viewModel = new MainWindowViewModel();
        string? requestedAnchor = null;
        viewModel.PreviewAnchorRequested += (_, slug) => requestedAnchor = slug;
        viewModel.OpenFile(filePath);

        viewModel.SelectedOutlineItem = viewModel.DocumentOutline.Single();

        Assert.Equal("heading-1", requestedAnchor);
        Assert.Equal("Outline: Heading 1", viewModel.StatusText);
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

    private sealed class RecordingUserSettingsService(UserSettings settings) : IUserSettingsService
    {
        public UserSettings? SavedSettings { get; private set; }

        public UserSettings Load() => settings;

        public void Save(UserSettings value)
        {
            SavedSettings = value;
        }
    }

    private sealed class RecordingUpdateCheckService(UpdateCheckResult result) : IUpdateCheckService
    {
        public bool Checked { get; private set; }

        public Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default)
        {
            Checked = true;
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingRecentItemsService : IRecentItemsService
    {
        public List<string> Paths { get; } = [];

        public IReadOnlyList<RecentItem> GetRecentItems()
        {
            return Paths.Select(path => new RecentItem(path, DateTimeOffset.UtcNow)).ToArray();
        }

        public void AddRecentItem(string path)
        {
            Paths.Add(Path.GetFullPath(path));
        }

        public void Clear()
        {
            Paths.Clear();
        }
    }

    private sealed class RecordingAutosaveRecoveryService : IAutosaveRecoveryService
    {
        public string? SnapshotDocumentPath { get; private set; }

        public string? SnapshotContent { get; private set; }

        public string? ClearedDocumentPath { get; private set; }

        public void WriteSnapshot(string documentPath, string content)
        {
            SnapshotDocumentPath = Path.GetFullPath(documentPath);
            SnapshotContent = content;
        }

        public IReadOnlyList<AutosaveRecoverySnapshot> ListRecoverableSnapshots()
        {
            return [];
        }

        public void ClearSnapshots(string documentPath)
        {
            ClearedDocumentPath = Path.GetFullPath(documentPath);
        }
    }

    private sealed class ManualPreviewRefreshScheduler : IPreviewRefreshScheduler
    {
        private Func<CancellationToken, Task>? latestRefresh;
        private CancellationTokenSource? latestTokenSource;

        public int ScheduledCount { get; private set; }

        public void Schedule(Func<CancellationToken, Task> refreshAsync)
        {
            latestTokenSource?.Cancel();
            latestTokenSource = new CancellationTokenSource();
            latestRefresh = refreshAsync;
            ScheduledCount++;
        }

        public void RunNow(Func<CancellationToken, Task> refreshAsync)
        {
            latestTokenSource?.Cancel();
            latestTokenSource = null;
            latestRefresh = null;
            refreshAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public void CancelPending()
        {
            latestTokenSource?.Cancel();
        }

        public async Task RunLatestAsync()
        {
            if (latestRefresh is null || latestTokenSource is null)
            {
                return;
            }

            await latestRefresh(latestTokenSource.Token);
        }

        public void Dispose()
        {
            latestTokenSource?.Dispose();
        }
    }
}
