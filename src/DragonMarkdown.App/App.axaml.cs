using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DragonMarkdown.App.Services;

namespace DragonMarkdown.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            var viewModel = new ViewModels.MainWindowViewModel(
                exportedDocumentOpener: new ExportedDocumentOpener(),
                userSettingsService: new UserSettingsService(AppDataPaths.SettingsPath),
                updateCheckService: new GitHubUpdateCheckService(httpClient));
            var startupPath = Program.StartupArgs.FirstOrDefault(argument => !argument.StartsWith("-", StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(startupPath))
            {
                viewModel.OpenPath(startupPath);
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
