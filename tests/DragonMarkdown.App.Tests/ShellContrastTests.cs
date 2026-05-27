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

    [Fact]
    public void ToolbarButtons_UseExplicitContrastOnDarkHeader()
    {
        var appXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "App.axaml");

        Assert.Contains("Style Selector=\"Button.toolbar-button\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Foreground\" Value=\"#F9FAFB\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"Background\" Value=\"#324154\"", appXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorTabs_UseCompactTenPointHeaders()
    {
        var appXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "App.axaml");
        var windowXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "MainWindow.axaml");

        Assert.Contains("Style Selector=\"TabItem\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"FontSize\" Value=\"10\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("Property=\"MinHeight\" Value=\"24\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"28\"", windowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_UsesRegularMenuBarForPrimaryCommands()
    {
        var windowXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "MainWindow.axaml");

        Assert.Contains("<Menu DockPanel.Dock=\"Top\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"_File\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportWordCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportPdfCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Header=\"_View\"", windowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_UsesHelpMenuForHelpAndAbout()
    {
        var windowXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "MainWindow.axaml");

        Assert.Contains("Header=\"_Help\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding OpenHelpCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowAboutCommand}\"", windowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_BindsDocumentPanesToExpansionLayoutProperties()
    {
        var windowXaml = ReadWorkspaceFile("src", "DragonMarkdown.App", "MainWindow.axaml");

        Assert.Contains("Grid.ColumnSpan=\"{Binding EditorColumnSpan}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"{Binding PreviewGridColumn}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Grid.ColumnSpan=\"{Binding PreviewColumnSpan}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding MiddleSplitterVisible}\"", windowXaml, StringComparison.Ordinal);
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
