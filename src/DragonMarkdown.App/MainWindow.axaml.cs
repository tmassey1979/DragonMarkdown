using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DragonMarkdown.App.Preview;
using DragonMarkdown.App.ViewModels;

namespace DragonMarkdown.App;

public partial class MainWindow : Window
{
    private readonly IPreviewHost previewHost;

    public MainWindow()
    {
        InitializeComponent();

        previewHost = new CefPreviewHost();
        this.FindControl<ContentControl>("PreviewHost")!.Content = previewHost.View;

        Opened += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.OpenFolderRequested += OnOpenFolderRequested;
                viewModel.OpenFileRequested += OnOpenFileRequested;
                viewModel.ExportWordRequested += OnExportWordRequested;
                viewModel.ExportPdfRequested += OnExportPdfRequested;
                viewModel.AboutRequested += OnAboutRequested;
                viewModel.PreviewAnchorRequested += OnPreviewAnchorRequested;
                previewHost.ShowHtml(MainWindowViewModel.EmptyPreviewHtml);
                viewModel.PreviewHtmlChanged += (_, html) => previewHost.ShowHtml(html);
            }
        };

        Closed += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.OpenFolderRequested -= OnOpenFolderRequested;
                viewModel.OpenFileRequested -= OnOpenFileRequested;
                viewModel.ExportWordRequested -= OnExportWordRequested;
                viewModel.ExportPdfRequested -= OnExportPdfRequested;
                viewModel.AboutRequested -= OnAboutRequested;
                viewModel.PreviewAnchorRequested -= OnPreviewAnchorRequested;
            }

            previewHost.Dispose();
        };
    }

    private async void OnOpenFolderRequested(object? sender, string e)
    {
        if (sender is not MainWindowViewModel viewModel)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new()
        {
            Title = "Open markdown folder",
            AllowMultiple = false
        });

        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } folderPath)
        {
            viewModel.OpenFolder(folderPath);
        }
    }

    private async void OnOpenFileRequested(object? sender, string e)
    {
        if (sender is not MainWindowViewModel viewModel)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new()
        {
            Title = "Open markdown file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new("Markdown")
                {
                    Patterns = ["*.md", "*.markdown", "*.mdown"]
                }
            ]
        });

        if (files.Count > 0 && files[0].TryGetLocalPath() is { } filePath)
        {
            viewModel.OpenFile(filePath);
        }
    }

    private async void OnExportWordRequested(object? sender, string suggestedFileName)
    {
        if (sender is not MainWindowViewModel viewModel)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new()
        {
            Title = "Export Word document",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "docx",
            FileTypeChoices =
            [
                new("Word document")
                {
                    Patterns = ["*.docx"]
                }
            ]
        });

        if (file?.TryGetLocalPath() is { } outputPath)
        {
            viewModel.ExportActiveDocumentToWord(outputPath);
        }
    }

    private async void OnExportPdfRequested(object? sender, string suggestedFileName)
    {
        if (sender is not MainWindowViewModel viewModel)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new()
        {
            Title = "Export PDF",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "pdf",
            FileTypeChoices =
            [
                new("PDF")
                {
                    Patterns = ["*.pdf"]
                }
            ]
        });

        if (file?.TryGetLocalPath() is { } outputPath)
        {
            viewModel.ExportActiveDocumentToPdf(outputPath);
        }
    }

    private void OnAboutRequested(object? sender, EventArgs e)
    {
        var aboutWindow = new AboutWindow();
        _ = aboutWindow.ShowDialog(this);
    }

    private void OnPreviewAnchorRequested(object? sender, string slug)
    {
        previewHost.ScrollToAnchor(slug);
    }
}
