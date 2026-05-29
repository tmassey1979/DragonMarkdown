using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonMarkdown.App.Services;
using DragonMarkdown.Core.Documents;
using DragonMarkdown.Core.Exporting;
using DragonMarkdown.Core.Health;
using DragonMarkdown.Core.Rendering;
using DragonMarkdown.Core.Workspaces;

namespace DragonMarkdown.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private const string ShellCoordinatorCommandContracts =
        "OpenSettingsCommand CheckForUpdatesCommand ExportWordCommand ExportPdfCommand OpenDocumentBacklinkCommand UpdateTableOfContentsCommand";

    public const string EmptyPreviewHtml = """
        <!doctype html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                body { font-family: Inter, Segoe UI, sans-serif; margin: 32px; color: #253041; }
                .empty { border: 1px solid #d8e0ea; border-radius: 8px; padding: 24px; background: #f7f9fc; }
            </style>
        </head>
        <body><div class="empty"><h1>DragonMarkdown</h1><p>Open a folder or file to start editing markdown.</p></div></body>
        </html>
        """;

    private readonly DocumentWorkspace documentWorkspace = new();
    private readonly MarkdownExporter exporter = new();
    private readonly MarkdownRenderer renderer = new();
    private readonly MarkdownOutlineBuilder outlineBuilder = new();
    private readonly MarkdownDocumentStatisticsService documentStatisticsService = new();
    private readonly MarkdownTableOfContentsService tableOfContentsService = new();
    private readonly WorkspaceSearchService workspaceSearchService = new();
    private readonly WorkspaceBacklinkService workspaceBacklinkService = new();
    private readonly WorkspaceStatisticsService workspaceStatisticsService = new();
    private readonly WorkspaceHealthAnalyzer workspaceHealthAnalyzer = new();
    private readonly ExportValidationService exportValidationService = new();
    private readonly IExportedDocumentOpener? exportedDocumentOpener;
    private readonly IUserSettingsService? userSettingsService;
    private readonly IUpdateCheckService? updateCheckService;
    private readonly IPreviewRefreshScheduler previewRefreshScheduler;
    private readonly IRecentItemsService? recentItemsService;
    private readonly IAutosaveRecoveryService? autosaveRecoveryService;
    private readonly string currentVersion;
    private readonly Dictionary<MarkdownDocument, OpenDocumentViewModel> openDocumentMap = [];
    private readonly string? helpDocumentPath;
    private int previewRefreshVersion;

    public MainWindowViewModel(
        string? helpDocumentPath = null,
        IExportedDocumentOpener? exportedDocumentOpener = null,
        IUserSettingsService? userSettingsService = null,
        IUpdateCheckService? updateCheckService = null,
        IPreviewRefreshScheduler? previewRefreshScheduler = null,
        IRecentItemsService? recentItemsService = null,
        IAutosaveRecoveryService? autosaveRecoveryService = null,
        string? currentVersion = null)
    {
        this.helpDocumentPath = helpDocumentPath ?? AppContentPaths.FindHelpDocumentPath();
        this.exportedDocumentOpener = exportedDocumentOpener;
        this.userSettingsService = userSettingsService;
        this.updateCheckService = updateCheckService;
        this.previewRefreshScheduler = previewRefreshScheduler ?? new DebouncedPreviewRefreshScheduler();
        this.recentItemsService = recentItemsService;
        this.autosaveRecoveryService = autosaveRecoveryService;
        this.currentVersion = currentVersion ?? GetCurrentVersion();
        WorkspaceItems = [];
        OpenDocuments = [];
        DocumentOutline = [];
        DocumentBacklinks = [];
        WorkspaceSearchResults = [];
        WorkspaceHealthIssues = [];
        ExportValidationIssues = [];
        RecentItems = [];
        RefreshRecentItems();
    }

    public ObservableCollection<WorkspaceNodeViewModel> WorkspaceItems { get; }

    public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

    public ObservableCollection<MarkdownOutlineItem> DocumentOutline { get; }

    public ObservableCollection<DocumentBacklinkViewModel> DocumentBacklinks { get; }

    public ObservableCollection<WorkspaceSearchResult> WorkspaceSearchResults { get; }

    public ObservableCollection<WorkspaceHealthIssueViewModel> WorkspaceHealthIssues { get; }

    public ObservableCollection<ExportValidationIssueViewModel> ExportValidationIssues { get; }

    public ObservableCollection<RecentItem> RecentItems { get; }

    [ObservableProperty]
    private WorkspaceNodeViewModel? selectedWorkspaceItem;

    [ObservableProperty]
    private OpenDocumentViewModel? selectedDocument;

    [ObservableProperty]
    private bool isEditorVisible = true;

    [ObservableProperty]
    private bool isPreviewVisible = true;

    [ObservableProperty]
    private string workspaceLabel = "No folder opened";

    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private string workspaceSearchText = string.Empty;

    [ObservableProperty]
    private WorkspaceSearchResult? selectedWorkspaceSearchResult;

    [ObservableProperty]
    private WorkspaceHealthIssueViewModel? selectedWorkspaceHealthIssue;

    [ObservableProperty]
    private string workspaceHealthSummary = "Docs health not analyzed";

    [ObservableProperty]
    private string workspaceStatisticsSummary = "No workspace statistics";

    [ObservableProperty]
    private string exportValidationSummary = "Export readiness not checked";

    [ObservableProperty]
    private string activeDocumentStatisticsSummary = "No document";

    [ObservableProperty]
    private string documentBacklinksSummary = "No backlinks";

    [ObservableProperty]
    private MarkdownOutlineItem? selectedOutlineItem;

    [ObservableProperty]
    private bool isSettingsOpen;

    [ObservableProperty]
    private string settingsTheme = UserSettings.Default.Theme;

    [ObservableProperty]
    private bool settingsWordWrap = UserSettings.Default.WordWrap;

    public string? WorkspaceRootPath { get; private set; }

    public int EditorColumnSpan => IsPreviewVisible ? 1 : 3;

    public int PreviewGridColumn => IsEditorVisible ? 4 : 2;

    public int PreviewColumnSpan => IsEditorVisible ? 1 : 3;

    public bool MiddleSplitterVisible => IsEditorVisible && IsPreviewVisible;

    public bool IsWorkspaceSearchActive => !string.IsNullOrWhiteSpace(WorkspaceSearchText);

    public bool HasWorkspaceHealthIssues => WorkspaceHealthIssues.Count > 0;

    public bool HasExportValidationIssues => ExportValidationIssues.Count > 0;

    public bool HasDocumentBacklinks => DocumentBacklinks.Count > 0;

    public event EventHandler<string>? OpenFolderRequested;

    public event EventHandler<string>? OpenFileRequested;

    public event EventHandler<string>? ExportWordRequested;

    public event EventHandler<string>? ExportPdfRequested;

    public event EventHandler? AboutRequested;

    public event EventHandler<string>? PreviewHtmlChanged;

    public event EventHandler<string>? PreviewAnchorRequested;

    partial void OnSelectedWorkspaceItemChanged(WorkspaceNodeViewModel? value)
    {
        if (value?.Kind == WorkspaceNodeKind.Markdown)
        {
            OpenDocument(value.FullPath);
        }
    }

    partial void OnSelectedDocumentChanged(OpenDocumentViewModel? value)
    {
        RefreshPreview();
    }

    partial void OnWorkspaceSearchTextChanged(string value)
    {
        RefreshWorkspaceSearch();
        OnPropertyChanged(nameof(IsWorkspaceSearchActive));
    }

    partial void OnSelectedWorkspaceSearchResultChanged(WorkspaceSearchResult? value)
    {
        if (value is null)
        {
            return;
        }

        OpenDocument(value.FullPath);
        SelectedWorkspaceSearchResult = null;
    }

    partial void OnSelectedWorkspaceHealthIssueChanged(WorkspaceHealthIssueViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        OpenWorkspaceHealthIssue(value);
        SelectedWorkspaceHealthIssue = null;
    }

    partial void OnSelectedOutlineItemChanged(MarkdownOutlineItem? value)
    {
        if (value is null)
        {
            return;
        }

        StatusText = $"Outline: {value.Title}";
        PreviewAnchorRequested?.Invoke(this, value.Slug);
    }

    partial void OnIsEditorVisibleChanged(bool value)
    {
        NotifyPaneLayoutChanged();
    }

    partial void OnIsPreviewVisibleChanged(bool value)
    {
        NotifyPaneLayoutChanged();
    }

    public void OpenPath(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            OpenFolder(fullPath);
            return;
        }

        if (File.Exists(fullPath))
        {
            OpenFile(fullPath);
            return;
        }

        StatusText = $"Path not found: {fullPath}";
    }

    public void OpenFolder(string folderPath)
    {
        WorkspaceRootPath = Path.GetFullPath(folderPath);
        WorkspaceLabel = WorkspaceRootPath;
        RefreshWorkspaceTree();
        RefreshWorkspaceSearch();
        RefreshWorkspaceStatistics();
        RefreshWorkspaceHealth();
        AddRecentItem(WorkspaceRootPath);
        StatusText = $"Opened folder {WorkspaceRootPath}";
    }

    public void OpenFile(string filePath)
    {
        WorkspaceRootPath ??= Path.GetDirectoryName(Path.GetFullPath(filePath));

        if (!string.IsNullOrWhiteSpace(WorkspaceRootPath) && Directory.Exists(WorkspaceRootPath))
        {
            WorkspaceLabel = WorkspaceRootPath;
            RefreshWorkspaceTree();
        }

        OpenDocument(filePath);
        AddRecentItem(Path.GetFullPath(filePath));
    }

    private void OpenDocument(string filePath)
    {
        try
        {
            var document = documentWorkspace.OpenDocument(filePath);
            if (!openDocumentMap.TryGetValue(document, out var viewModel))
            {
                viewModel = new OpenDocumentViewModel(document);
                viewModel.TextChanged += (_, _) => QueuePreviewRefresh();
                openDocumentMap[document] = viewModel;
                OpenDocuments.Add(viewModel);
            }

            SelectedDocument = viewModel;
            StatusText = $"Opened {document.DisplayName}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StatusText = ex.Message;
        }
    }

    private void RefreshWorkspaceTree()
    {
        WorkspaceItems.Clear();

        if (WorkspaceRootPath is null || !Directory.Exists(WorkspaceRootPath))
        {
            return;
        }

        var workspace = WorkspaceTreeBuilder.Build(WorkspaceRootPath);
        foreach (var child in workspace.Children)
        {
            WorkspaceItems.Add(WorkspaceNodeViewModel.FromWorkspaceItem(child));
        }
    }

    private void RefreshPreview()
    {
        Interlocked.Increment(ref previewRefreshVersion);

        if (SelectedDocument is null)
        {
            DocumentOutline.Clear();
            DocumentBacklinks.Clear();
            ActiveDocumentStatisticsSummary = "No document";
            DocumentBacklinksSummary = "No backlinks";
            OnPropertyChanged(nameof(HasDocumentBacklinks));
            PreviewHtmlChanged?.Invoke(this, EmptyPreviewHtml);
            return;
        }

        var workspaceRoot = WorkspaceRootPath
            ?? Path.GetDirectoryName(SelectedDocument.Document.FilePath)
            ?? Environment.CurrentDirectory;

        var result = renderer.RenderDocument(
            SelectedDocument.Text,
            new MarkdownRenderOptions(workspaceRoot, SelectedDocument.Document.FilePath));

        RefreshDocumentOutline(SelectedDocument.Text);
        RefreshDocumentStatistics(SelectedDocument.Text);
        RefreshDocumentBacklinks(SelectedDocument.Document.FilePath);
        PreviewHtmlChanged?.Invoke(this, result.Html);
    }

    private void QueuePreviewRefresh()
    {
        if (SelectedDocument is null)
        {
            RefreshPreview();
            return;
        }

        int version = Interlocked.Increment(ref previewRefreshVersion);
        string markdown = SelectedDocument.Text;
        string documentPath = SelectedDocument.Document.FilePath;
        string workspaceRoot = WorkspaceRootPath
            ?? Path.GetDirectoryName(documentPath)
            ?? Environment.CurrentDirectory;
        AutosaveDocument(SelectedDocument);
        ClearExportValidation();

        previewRefreshScheduler.Schedule(
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = renderer.RenderDocument(markdown, new MarkdownRenderOptions(workspaceRoot, documentPath));
                cancellationToken.ThrowIfCancellationRequested();

                if (version != Volatile.Read(ref previewRefreshVersion))
                {
                    return Task.CompletedTask;
                }

                RefreshDocumentOutline(markdown);
                RefreshDocumentStatistics(markdown);
                PreviewHtmlChanged?.Invoke(this, result.Html);
                return Task.CompletedTask;
            });
    }

    private void AutosaveDocument(OpenDocumentViewModel document)
    {
        autosaveRecoveryService?.WriteSnapshot(document.Document.FilePath, document.Text);
    }

    private void RefreshDocumentOutline(string markdown)
    {
        SelectedOutlineItem = null;
        DocumentOutline.Clear();
        foreach (var item in outlineBuilder.Build(markdown))
        {
            DocumentOutline.Add(item);
        }
    }

    private void RefreshDocumentStatistics(string markdown)
    {
        var statistics = documentStatisticsService.Analyze(markdown);
        ActiveDocumentStatisticsSummary = $"{statistics.WordCount} {Pluralize(statistics.WordCount, "word")} | "
            + $"{statistics.HeadingCount} {Pluralize(statistics.HeadingCount, "heading")} | "
            + $"{statistics.LinkCount} {Pluralize(statistics.LinkCount, "link")} | "
            + $"{statistics.ImageCount} {Pluralize(statistics.ImageCount, "image")} | "
            + $"{statistics.EstimatedReadingMinutes} min read";
    }

    private void RefreshDocumentBacklinks(string documentPath)
    {
        DocumentBacklinks.Clear();

        if (string.IsNullOrWhiteSpace(WorkspaceRootPath) || !Directory.Exists(WorkspaceRootPath))
        {
            DocumentBacklinksSummary = "No backlinks";
            OnPropertyChanged(nameof(HasDocumentBacklinks));
            return;
        }

        foreach (var backlink in workspaceBacklinkService.FindBacklinks(WorkspaceRootPath, documentPath))
        {
            DocumentBacklinks.Add(new DocumentBacklinkViewModel(backlink));
        }

        DocumentBacklinksSummary = $"{DocumentBacklinks.Count} {Pluralize(DocumentBacklinks.Count, "backlink")}";
        OnPropertyChanged(nameof(HasDocumentBacklinks));
    }

    private void RefreshWorkspaceSearch()
    {
        WorkspaceSearchResults.Clear();

        if (string.IsNullOrWhiteSpace(WorkspaceSearchText)
            || string.IsNullOrWhiteSpace(WorkspaceRootPath)
            || !Directory.Exists(WorkspaceRootPath))
        {
            return;
        }

        foreach (var result in workspaceSearchService.Search(WorkspaceRootPath, WorkspaceSearchText))
        {
            WorkspaceSearchResults.Add(result);
        }
    }

    private void RefreshWorkspaceStatistics()
    {
        if (string.IsNullOrWhiteSpace(WorkspaceRootPath) || !Directory.Exists(WorkspaceRootPath))
        {
            WorkspaceStatisticsSummary = "No workspace statistics";
            return;
        }

        var statistics = workspaceStatisticsService.Analyze(WorkspaceRootPath);
        WorkspaceStatisticsSummary = $"{statistics.DocumentCount} {Pluralize(statistics.DocumentCount, "document")} | "
            + $"{statistics.WordCount} {Pluralize(statistics.WordCount, "word")} | "
            + $"{statistics.HeadingCount} {Pluralize(statistics.HeadingCount, "heading")} | "
            + $"{statistics.EstimatedReadingMinutes} min read";
    }

    [RelayCommand]
    private void RefreshWorkspaceHealth()
    {
        WorkspaceHealthIssues.Clear();

        if (string.IsNullOrWhiteSpace(WorkspaceRootPath) || !Directory.Exists(WorkspaceRootPath))
        {
            WorkspaceHealthSummary = "Docs health not analyzed";
            StatusText = "Open a folder before analyzing docs health.";
            OnPropertyChanged(nameof(HasWorkspaceHealthIssues));
            return;
        }

        var report = workspaceHealthAnalyzer.Analyze(WorkspaceRootPath);
        foreach (var issue in report.Issues)
        {
            WorkspaceHealthIssues.Add(new WorkspaceHealthIssueViewModel(issue, WorkspaceRootPath));
        }

        WorkspaceHealthSummary = FormatWorkspaceHealthSummary(report);
        StatusText = WorkspaceHealthSummary;
        OnPropertyChanged(nameof(HasWorkspaceHealthIssues));
    }

    public void ExportActiveDocumentToWord(string outputPath)
    {
        if (!TryExportActiveDocument(outputPath, ExportFormat.Word, out _))
        {
            return;
        }

        StatusText = $"Exported Word document {Path.GetFileName(outputPath)}";
        OpenExportedDocument(outputPath);
    }

    public void ExportActiveDocumentToPdf(string outputPath)
    {
        if (!TryExportActiveDocument(outputPath, ExportFormat.Pdf, out _))
        {
            return;
        }

        StatusText = $"Exported PDF {Path.GetFileName(outputPath)}";
        OpenExportedDocument(outputPath);
    }

    private void RefreshRecentItems()
    {
        RecentItems.Clear();
        foreach (var item in recentItemsService?.GetRecentItems() ?? [])
        {
            RecentItems.Add(item);
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settings = userSettingsService?.Load() ?? UserSettings.Default;
        SettingsTheme = settings.Theme;
        SettingsWordWrap = settings.WordWrap;
        IsSettingsOpen = true;
        StatusText = "Settings ready.";
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        userSettingsService?.Save(new UserSettings(WorkspaceRootPath, SettingsTheme, SettingsWordWrap));
        IsSettingsOpen = false;
        StatusText = "Settings saved.";
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        if (updateCheckService is null)
        {
            StatusText = "Update check is not configured.";
            return;
        }

        StatusText = "Checking for updates...";
        var result = await updateCheckService.CheckForUpdatesAsync(currentVersion);
        StatusText = result.Message;
    }

    [RelayCommand]
    private void OpenWorkspaceSearchResult(WorkspaceSearchResult? result)
    {
        if (result is null)
        {
            return;
        }

        OpenDocument(result.FullPath);
    }

    [RelayCommand]
    private void OpenDocumentBacklink(DocumentBacklinkViewModel? backlink)
    {
        if (backlink is null)
        {
            return;
        }

        OpenDocument(backlink.FullPath);
    }

    [RelayCommand]
    private void UpdateTableOfContents()
    {
        if (SelectedDocument is null)
        {
            StatusText = "No active document to update.";
            return;
        }

        SelectedDocument.Text = tableOfContentsService.UpdateTableOfContents(SelectedDocument.Text);
        StatusText = "Updated table of contents.";
    }

    [RelayCommand]
    private void ValidateExportReadiness()
    {
        if (SelectedDocument is null)
        {
            StatusText = "No active document to validate.";
            ExportValidationSummary = "Export readiness not checked";
            ExportValidationIssues.Clear();
            OnPropertyChanged(nameof(HasExportValidationIssues));
            return;
        }

        var request = CreateExportRequest(SelectedDocument, SelectedDocument.Document.FilePath, ExportFormat.Pdf);
        var report = exportValidationService.Validate(request);
        ApplyExportValidationReport(report);
        StatusText = ExportValidationSummary;
    }

    [RelayCommand]
    private void OpenWorkspaceHealthIssue(WorkspaceHealthIssueViewModel? issue)
    {
        if (issue is null)
        {
            return;
        }

        if (!File.Exists(issue.DocumentPath))
        {
            StatusText = $"Health issue document not found: {issue.RelativePath}";
            return;
        }

        OpenDocument(issue.DocumentPath);
    }

    [RelayCommand]
    private void OpenRecentItem(RecentItem? recentItem)
    {
        if (recentItem is null)
        {
            return;
        }

        OpenPath(recentItem.Path);
    }

    [RelayCommand]
    private void ToggleEditor()
    {
        if (IsEditorVisible && !IsPreviewVisible)
        {
            StatusText = "Keep either the editor or preview visible.";
            return;
        }

        IsEditorVisible = !IsEditorVisible;
        StatusText = IsEditorVisible ? "Editor visible" : "Editor hidden";
    }

    [RelayCommand]
    private void TogglePreview()
    {
        if (IsPreviewVisible && !IsEditorVisible)
        {
            StatusText = "Keep either the editor or preview visible.";
            return;
        }

        IsPreviewVisible = !IsPreviewVisible;
        StatusText = IsPreviewVisible ? "Preview visible" : "Preview hidden";
    }

    [RelayCommand]
    private void OpenFolder()
    {
        OpenFolderRequested?.Invoke(this, "Open folder");
    }

    [RelayCommand]
    private void OpenFile()
    {
        OpenFileRequested?.Invoke(this, "Open file");
    }

    [RelayCommand]
    private void ExportWord()
    {
        if (SelectedDocument is null)
        {
            StatusText = "No active document to export.";
            return;
        }

        ExportWordRequested?.Invoke(this, GetSuggestedExportFileName(".docx"));
    }

    [RelayCommand]
    private void ExportPdf()
    {
        if (SelectedDocument is null)
        {
            StatusText = "No active document to export.";
            return;
        }

        ExportPdfRequested?.Invoke(this, GetSuggestedExportFileName(".pdf"));
    }

    [RelayCommand]
    private void OpenHelp()
    {
        if (string.IsNullOrWhiteSpace(helpDocumentPath) || !File.Exists(helpDocumentPath))
        {
            StatusText = "Help document not found.";
            return;
        }

        OpenFile(helpDocumentPath);
    }

    [RelayCommand]
    private void ShowAbout()
    {
        AboutRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SaveActive()
    {
        if (SelectedDocument is null)
        {
            StatusText = "No active document to save.";
            return;
        }

        SelectedDocument.Save();
        autosaveRecoveryService?.ClearSnapshots(SelectedDocument.Document.FilePath);
        StatusText = $"Saved {SelectedDocument.DisplayName}";
    }

    [RelayCommand]
    private void SaveAll()
    {
        foreach (var document in OpenDocuments)
        {
            document.Save();
            autosaveRecoveryService?.ClearSnapshots(document.Document.FilePath);
        }

        StatusText = "Saved all open files";
    }

    [RelayCommand]
    private void CreateFile()
    {
        if (!TryGetTargetFolder(out var folderPath))
        {
            StatusText = "Open a folder before creating files.";
            return;
        }

        var filePath = GetUniquePath(folderPath, "Untitled", ".md");
        File.WriteAllText(filePath, "# Untitled" + Environment.NewLine);
        RefreshWorkspaceTree();
        OpenDocument(filePath);
    }

    [RelayCommand]
    private void CreateFolder()
    {
        if (!TryGetTargetFolder(out var folderPath))
        {
            StatusText = "Open a folder before creating folders.";
            return;
        }

        Directory.CreateDirectory(GetUniquePath(folderPath, "New Folder", string.Empty));
        RefreshWorkspaceTree();
        StatusText = "Created folder.";
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedWorkspaceItem is null)
        {
            StatusText = "Select a file or folder to delete.";
            return;
        }

        if (SelectedWorkspaceItem.Kind == WorkspaceNodeKind.Folder)
        {
            Directory.Delete(SelectedWorkspaceItem.FullPath, recursive: true);
        }
        else
        {
            File.Delete(SelectedWorkspaceItem.FullPath);
        }

        RefreshWorkspaceTree();
        StatusText = $"Deleted {SelectedWorkspaceItem.Name}";
    }

    [RelayCommand]
    private void RevealSelected()
    {
        if (SelectedWorkspaceItem is null)
        {
            StatusText = "Select a file or folder to reveal.";
            return;
        }

        var argument = OperatingSystem.IsWindows()
            ? $"/select,\"{SelectedWorkspaceItem.FullPath}\""
            : $"\"{SelectedWorkspaceItem.FullPath}\"";

        var fileName = OperatingSystem.IsWindows() ? "explorer.exe" : "open";
        Process.Start(new ProcessStartInfo(fileName, argument)
        {
            UseShellExecute = true
        });
    }

    private bool TryGetTargetFolder(out string folderPath)
    {
        folderPath = SelectedWorkspaceItem is { Kind: WorkspaceNodeKind.Folder } folder
            ? folder.FullPath
            : Path.GetDirectoryName(SelectedWorkspaceItem?.FullPath ?? string.Empty) ?? WorkspaceRootPath ?? string.Empty;

        return !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath);
    }

    private bool TryGetActiveExport(
        out OpenDocumentViewModel document,
        out MarkdownRenderOptions options)
    {
        if (SelectedDocument is null)
        {
            document = null!;
            options = null!;
            StatusText = "No active document to export.";
            return false;
        }

        document = SelectedDocument;
        options = CreateRenderOptions(document);
        return true;
    }

    private bool TryExportActiveDocument(string outputPath, ExportFormat format, out ExportResult result)
    {
        result = null!;

        if (!TryGetActiveExport(out var document, out var options))
        {
            return false;
        }

        var request = CreateExportRequest(document, outputPath, format);
        result = exporter.Export(request, options);
        ApplyExportValidationReport(result.ValidationReport);

        if (result.Succeeded)
        {
            return true;
        }

        StatusText = result.ErrorMessage == "Export validation failed."
            ? $"Export validation failed: {FormatExportValidationCounts(result.ValidationReport)}"
            : $"Export failed: {result.ErrorMessage}";
        return false;
    }

    private static MarkdownExportRequest CreateExportRequest(
        OpenDocumentViewModel document,
        string outputPath,
        ExportFormat format)
    {
        var profile = format == ExportFormat.Word
            ? ExportProfile.Word("Word")
            : ExportProfile.Pdf("PDF");

        return new MarkdownExportRequest(
            document.Text,
            document.Document.FilePath,
            outputPath,
            format,
            profile);
    }

    private MarkdownRenderOptions CreateRenderOptions(OpenDocumentViewModel document)
    {
        var workspaceRoot = WorkspaceRootPath
            ?? Path.GetDirectoryName(document.Document.FilePath)
            ?? Environment.CurrentDirectory;

        return new MarkdownRenderOptions(workspaceRoot, document.Document.FilePath);
    }

    private string GetSuggestedExportFileName(string extension)
    {
        var fileName = SelectedDocument?.DisplayName ?? "document.md";
        return Path.ChangeExtension(fileName, extension);
    }

    private void OpenExportedDocument(string outputPath)
    {
        if (exportedDocumentOpener is null)
        {
            return;
        }

        var openResult = exportedDocumentOpener.Open(outputPath);
        if (openResult.Succeeded)
        {
            StatusText += " and opened it.";
            return;
        }

        StatusText += $" but could not open it: {openResult.ErrorMessage}";
    }

    private void NotifyPaneLayoutChanged()
    {
        OnPropertyChanged(nameof(EditorColumnSpan));
        OnPropertyChanged(nameof(PreviewGridColumn));
        OnPropertyChanged(nameof(PreviewColumnSpan));
        OnPropertyChanged(nameof(MiddleSplitterVisible));
    }

    private void AddRecentItem(string path)
    {
        recentItemsService?.AddRecentItem(path);
        RefreshRecentItems();
    }

    private void ApplyExportValidationReport(ExportValidationReport report)
    {
        ExportValidationIssues.Clear();

        foreach (var issue in report.Errors)
        {
            ExportValidationIssues.Add(new ExportValidationIssueViewModel(issue, "Error"));
        }

        foreach (var issue in report.Warnings)
        {
            ExportValidationIssues.Add(new ExportValidationIssueViewModel(issue, "Warning"));
        }

        ExportValidationSummary = report.Errors.Count == 0 && report.Warnings.Count == 0
            ? "Export readiness clean"
            : $"Export readiness: {FormatExportValidationCounts(report)}";
        OnPropertyChanged(nameof(HasExportValidationIssues));
    }

    private void ClearExportValidation()
    {
        ExportValidationIssues.Clear();
        ExportValidationSummary = "Export readiness not checked";
        OnPropertyChanged(nameof(HasExportValidationIssues));
    }

    private static string GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "0.1.0.3";
    }

    private static string FormatWorkspaceHealthSummary(WorkspaceHealthReport report)
    {
        if (report.Issues.Count == 0)
        {
            return "Docs health clean";
        }

        return $"Docs health: {report.ErrorCount} {Pluralize(report.ErrorCount, "error")}, "
            + $"{report.WarningCount} {Pluralize(report.WarningCount, "warning")}, "
            + $"{report.InfoCount} {Pluralize(report.InfoCount, "note")}";
    }

    private static string Pluralize(int count, string noun) => count == 1 ? noun : noun + "s";

    private static string FormatExportValidationCounts(ExportValidationReport report) =>
        $"{report.Errors.Count} {Pluralize(report.Errors.Count, "error")}, "
        + $"{report.Warnings.Count} {Pluralize(report.Warnings.Count, "warning")}";

    private static string GetUniquePath(string folderPath, string baseName, string extension)
    {
        var candidate = Path.Combine(folderPath, baseName + extension);
        var index = 2;

        while (File.Exists(candidate) || Directory.Exists(candidate))
        {
            candidate = Path.Combine(folderPath, $"{baseName} {index}{extension}");
            index++;
        }

        return candidate;
    }
}
