using System.Text.RegularExpressions;

namespace DragonMarkdown.App.Tests;

public sealed class StaticQualityGateTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("src/DragonMarkdown.App/ViewModels/OpenDocumentViewModel.cs", "OpenDocumentViewModel")]
    [InlineData("src/DragonMarkdown.App/Services/IUserSettingsService.cs", "IUserSettingsService")]
    [InlineData("src/DragonMarkdown.App/Services/UserSettings.cs", "UserSettings")]
    [InlineData("src/DragonMarkdown.App/Services/IUpdateCheckService.cs", "IUpdateCheckService")]
    [InlineData("src/DragonMarkdown.App/Services/UpdateCheckResult.cs", "UpdateCheckResult")]
    [InlineData("src/DragonMarkdown.App/Services/IPreviewRefreshScheduler.cs", "IPreviewRefreshScheduler")]
    [InlineData("src/DragonMarkdown.App/ViewModels/WorkspaceNodeKind.cs", "WorkspaceNodeKind")]
    [InlineData("src/DragonMarkdown.App/ViewModels/WorkspaceNodeViewModel.cs", "WorkspaceNodeViewModel")]
    [InlineData("src/DragonMarkdown.Core/Rendering/BlockedMarkdownReference.cs", "BlockedMarkdownReference")]
    [InlineData("src/DragonMarkdown.Core/Rendering/MarkdownReferenceBlockReason.cs", "MarkdownReferenceBlockReason")]
    [InlineData("src/DragonMarkdown.Core/Rendering/MarkdownReferenceKind.cs", "MarkdownReferenceKind")]
    [InlineData("src/DragonMarkdown.Core/Workspaces/WorkspaceItemKind.cs", "WorkspaceItemKind")]
    public void KnownMovedTypesStayInDedicatedFiles(string relativePath, string typeName)
    {
        var source = Read(relativePath);

        Assert.Matches(TypeDeclarationPattern(typeName), source);
    }

    [Theory]
    [InlineData("src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs", "OpenDocumentViewModel")]
    [InlineData("src/DragonMarkdown.App/Services/UserSettingsService.cs", "IUserSettingsService")]
    [InlineData("src/DragonMarkdown.App/Services/UserSettingsService.cs", "UserSettings")]
    [InlineData("src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs", "WorkspaceNodeKind")]
    [InlineData("src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs", "WorkspaceNodeViewModel")]
    [InlineData("src/DragonMarkdown.Core/Rendering/MarkdownRenderResult.cs", "BlockedMarkdownReference")]
    [InlineData("src/DragonMarkdown.Core/Rendering/MarkdownRenderResult.cs", "MarkdownReferenceBlockReason")]
    [InlineData("src/DragonMarkdown.Core/Rendering/MarkdownRenderResult.cs", "MarkdownReferenceKind")]
    [InlineData("src/DragonMarkdown.Core/Workspaces/WorkspaceItem.cs", "WorkspaceItemKind")]
    public void KnownMovedTypesDoNotRegressIntoFormerContainerFiles(string relativePath, string typeName)
    {
        var source = Read(relativePath);

        Assert.DoesNotMatch(TypeDeclarationPattern(typeName), source);
    }

    [Fact]
    public void ShellXamlExposesV0102CoordinatorHooks()
    {
        var windowXaml = Read("src/DragonMarkdown.App/MainWindow.axaml");

        Assert.Contains("Name=\"MainToolbar\"", windowXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CommandPalette", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding OpenSettingsCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CheckForUpdatesCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding WorkspaceSearchText, Mode=TwoWay}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding WorkspaceSearchResults}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("OpenWorkspaceSearchResultCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding DocumentOutline}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedOutlineItem, Mode=TwoWay}\"", windowXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemsControl Grid.Column=\"1\" ItemsSource=\"{Binding DocumentOutline}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Name=\"EditorTextBox\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsSettingsOpen}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportWordCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportPdfCommand}\"", windowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModelExposesV0102ShellCoordinatorContracts()
    {
        var viewModelSource = Read("src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs");

        Assert.DoesNotContain("CommandPalette", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("CheckForUpdatesCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("WorkspaceSearchText", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("DocumentOutline", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportWordCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportPdfCommand", viewModelSource, StringComparison.Ordinal);
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    }

    private static Regex TypeDeclarationPattern(string typeName)
    {
        return new Regex($@"\b(public|internal|private)\s+(sealed\s+|abstract\s+|partial\s+)*?(class|record|enum|interface)\s+{Regex.Escape(typeName)}\b");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DragonMarkdown.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find DragonMarkdown repository root.");
    }
}
