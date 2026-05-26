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
}
