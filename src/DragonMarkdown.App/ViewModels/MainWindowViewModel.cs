using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonMarkdown.Core.Documents;
using DragonMarkdown.Core.Rendering;
using DragonMarkdown.Core.Workspaces;

namespace DragonMarkdown.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
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
    private readonly MarkdownRenderer renderer = new();
    private readonly Dictionary<MarkdownDocument, OpenDocumentViewModel> openDocumentMap = [];

    public MainWindowViewModel()
    {
        WorkspaceItems = [];
        OpenDocuments = [];
    }

    public ObservableCollection<WorkspaceNodeViewModel> WorkspaceItems { get; }

    public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; }

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

    public string? WorkspaceRootPath { get; private set; }

    public event EventHandler<string>? OpenFolderRequested;

    public event EventHandler<string>? OpenFileRequested;

    public event EventHandler<string>? PreviewHtmlChanged;

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

    public void OpenFolder(string folderPath)
    {
        WorkspaceRootPath = Path.GetFullPath(folderPath);
        WorkspaceLabel = WorkspaceRootPath;
        RefreshWorkspaceTree();
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
    }

    private void OpenDocument(string filePath)
    {
        try
        {
            var document = documentWorkspace.OpenDocument(filePath);
            if (!openDocumentMap.TryGetValue(document, out var viewModel))
            {
                viewModel = new OpenDocumentViewModel(document);
                viewModel.TextChanged += (_, _) => RefreshPreview();
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
        if (SelectedDocument is null)
        {
            PreviewHtmlChanged?.Invoke(this, EmptyPreviewHtml);
            return;
        }

        var workspaceRoot = WorkspaceRootPath
            ?? Path.GetDirectoryName(SelectedDocument.Document.FilePath)
            ?? Environment.CurrentDirectory;

        var result = renderer.RenderDocument(
            SelectedDocument.Text,
            new MarkdownRenderOptions(workspaceRoot, SelectedDocument.Document.FilePath));

        PreviewHtmlChanged?.Invoke(this, result.Html);
    }

    [RelayCommand]
    private void ToggleEditor()
    {
        IsEditorVisible = !IsEditorVisible;
        StatusText = IsEditorVisible ? "Editor visible" : "Editor hidden";
    }

    [RelayCommand]
    private void TogglePreview()
    {
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
    private void SaveActive()
    {
        if (SelectedDocument is null)
        {
            StatusText = "No active document to save.";
            return;
        }

        SelectedDocument.Save();
        StatusText = $"Saved {SelectedDocument.DisplayName}";
    }

    [RelayCommand]
    private void SaveAll()
    {
        foreach (var document in OpenDocuments)
        {
            document.Save();
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

public sealed partial class OpenDocumentViewModel : ObservableObject
{
    public OpenDocumentViewModel(MarkdownDocument document)
    {
        Document = document;
        text = document.Text;
    }

    public MarkdownDocument Document { get; }

    public string DisplayName => Document.DisplayName;

    public bool IsDirty => Document.IsDirty;

    [ObservableProperty]
    private string text;

    public event EventHandler? TextChanged;

    partial void OnTextChanged(string value)
    {
        Document.UpdateText(value);
        OnPropertyChanged(nameof(IsDirty));
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        Document.Save();
        OnPropertyChanged(nameof(IsDirty));
    }
}

public sealed class WorkspaceNodeViewModel
{
    public WorkspaceNodeViewModel(
        string name,
        string fullPath,
        string relativePath,
        WorkspaceNodeKind kind,
        IEnumerable<WorkspaceNodeViewModel>? children = null)
    {
        Name = name;
        FullPath = fullPath;
        RelativePath = relativePath;
        Kind = kind;
        Children = new ObservableCollection<WorkspaceNodeViewModel>(children ?? []);
    }

    public string Name { get; }

    public string FullPath { get; }

    public string RelativePath { get; }

    public WorkspaceNodeKind Kind { get; }

    public ObservableCollection<WorkspaceNodeViewModel> Children { get; }

    public string Glyph => Kind switch
    {
        WorkspaceNodeKind.Folder => "DIR",
        WorkspaceNodeKind.Markdown => "MD",
        _ => "FILE"
    };

    public static WorkspaceNodeViewModel FromWorkspaceItem(WorkspaceItem item)
    {
        var kind = item.Kind switch
        {
            WorkspaceItemKind.Folder => WorkspaceNodeKind.Folder,
            WorkspaceItemKind.MarkdownFile => WorkspaceNodeKind.Markdown,
            _ => WorkspaceNodeKind.Asset
        };

        return new WorkspaceNodeViewModel(
            item.Name,
            item.FullPath,
            item.RelativePath,
            kind,
            item.Children.Select(FromWorkspaceItem));
    }
}

public enum WorkspaceNodeKind
{
    Folder,
    Markdown,
    Asset
}
