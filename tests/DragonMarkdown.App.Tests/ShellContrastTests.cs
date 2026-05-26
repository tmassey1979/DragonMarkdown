namespace DragonMarkdown.App.Tests;

public sealed class ShellContrastTests
{
    [Fact]
    public void App_UsesLightThemeForLightWorkbenchPanels()
    {
        var appXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "App.axaml");

        Assert.Contains("RequestedThemeVariant=\"Light\"", appXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TreeTemplate_UsesExplicitDarkTextOnWhitePanel()
    {
        var windowXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "MainWindow.axaml");

        Assert.Contains("Foreground=\"#253041\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"#617184\"", windowXaml, StringComparison.Ordinal);
    }

    private static string ReadWorkspaceFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DragonMarkdown.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine([directory.FullName, .. segments]));
    }
}
