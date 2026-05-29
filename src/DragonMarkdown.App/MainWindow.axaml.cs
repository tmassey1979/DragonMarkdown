using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DragonMarkdown.App.Preview;
using DragonMarkdown.App.ViewModels;

namespace DragonMarkdown.App;

public partial class MainWindow : Window
{
    private readonly IPreviewHost previewHost;
    private readonly DispatcherTimer previewScrollTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private ScrollViewer? editorScrollViewer;
    private bool suppressEditorScroll;

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

            InitializeScrollSync();
            previewScrollTimer.Start();
        };

        previewScrollTimer.Tick += OnPreviewScrollTimerTick;

        Closed += (_, _) =>
        {
            previewScrollTimer.Stop();
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

    private void InitializeScrollSync()
    {
        var editorTextBox = this.FindControl<TextBox>("EditorTextBox");
        editorScrollViewer = editorTextBox?
            .GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();

        if (editorScrollViewer is not null)
        {
            editorScrollViewer.ScrollChanged += OnEditorScrollChanged;
        }
    }

    private void OnEditorScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (suppressEditorScroll || editorScrollViewer is null)
        {
            return;
        }

        previewHost.ScrollToRatio(GetEditorScrollRatio());
    }

    private async void OnPreviewScrollTimerTick(object? sender, EventArgs e)
    {
        if (editorScrollViewer is null || suppressEditorScroll)
        {
            return;
        }

        double? previewRatio = await previewHost.GetScrollRatioAsync();
        if (previewRatio is null)
        {
            return;
        }

        double editorRatio = GetEditorScrollRatio();
        if (Math.Abs(previewRatio.Value - editorRatio) < 0.03)
        {
            return;
        }

        ScrollEditorToRatio(previewRatio.Value);
    }

    private double GetEditorScrollRatio()
    {
        if (editorScrollViewer is null)
        {
            return 0;
        }

        double scrollableHeight = Math.Max(0, editorScrollViewer.Extent.Height - editorScrollViewer.Viewport.Height);
        return scrollableHeight <= 0
            ? 0
            : Math.Clamp(editorScrollViewer.Offset.Y / scrollableHeight, 0, 1);
    }

    private void ScrollEditorToRatio(double ratio)
    {
        if (editorScrollViewer is null)
        {
            return;
        }

        double scrollableHeight = Math.Max(0, editorScrollViewer.Extent.Height - editorScrollViewer.Viewport.Height);
        suppressEditorScroll = true;
        try
        {
            editorScrollViewer.Offset = new Vector(
                editorScrollViewer.Offset.X,
                scrollableHeight * Math.Clamp(ratio, 0, 1));
        }
        finally
        {
            suppressEditorScroll = false;
        }
    }
}
