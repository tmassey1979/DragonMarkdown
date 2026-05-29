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
    [InlineData("src/DragonMarkdown.App/ViewModels/WorkspaceHealthIssueViewModel.cs", "WorkspaceHealthIssueViewModel")]
    [InlineData("src/DragonMarkdown.App/ViewModels/ExportValidationIssueViewModel.cs", "ExportValidationIssueViewModel")]
    [InlineData("src/DragonMarkdown.App/ViewModels/DocumentBacklinkViewModel.cs", "DocumentBacklinkViewModel")]
    [InlineData("src/DragonMarkdown.App/Services/IGitWorkspaceStatusService.cs", "IGitWorkspaceStatusService")]
    [InlineData("src/DragonMarkdown.App/Services/GitWorkspaceStatus.cs", "GitWorkspaceStatus")]
    [InlineData("src/DragonMarkdown.Core/Health/WorkspaceHealthIssue.cs", "WorkspaceHealthIssue")]
    [InlineData("src/DragonMarkdown.Core/Health/WorkspaceHealthIssueSeverity.cs", "WorkspaceHealthIssueSeverity")]
    [InlineData("src/DragonMarkdown.Core/Health/WorkspaceHealthReport.cs", "WorkspaceHealthReport")]
    [InlineData("src/DragonMarkdown.Core/Documents/MarkdownDocumentStatistics.cs", "MarkdownDocumentStatistics")]
    [InlineData("src/DragonMarkdown.Core/Documents/MarkdownDocumentStatisticsService.cs", "MarkdownDocumentStatisticsService")]
    [InlineData("src/DragonMarkdown.Core/Documents/MarkdownTableOfContentsService.cs", "MarkdownTableOfContentsService")]
    [InlineData("src/DragonMarkdown.Core/Workspaces/WorkspaceBacklink.cs", "WorkspaceBacklink")]
    [InlineData("src/DragonMarkdown.Core/Workspaces/WorkspaceBacklinkService.cs", "WorkspaceBacklinkService")]
    [InlineData("src/DragonMarkdown.Core/Workspaces/WorkspaceStatistics.cs", "WorkspaceStatistics")]
    [InlineData("src/DragonMarkdown.Core/Workspaces/WorkspaceStatisticsService.cs", "WorkspaceStatisticsService")]
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
        Assert.Contains("RefreshWorkspaceHealthCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding WorkspaceHealthIssues}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding WorkspaceHealthSummary}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding WorkspaceStatisticsSummary}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding GitWorkspaceSummary}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("RefreshGitWorkspaceStatusCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("<ListBox DockPanel.Dock=\"Top\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("<TreeView ItemsSource=\"{Binding WorkspaceItems}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Foreground=\"#253041\" />", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ValidateExportReadinessCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding ExportValidationIssues}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ExportValidationSummary}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ActiveDocumentStatisticsSummary}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding DocumentBacklinks}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("OpenDocumentBacklinkCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("UpdateTableOfContentsCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("InsertTableCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("InsertMermaidDiagramCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("InsertImageCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding DocumentOutline}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedOutlineItem, Mode=TwoWay}\"", windowXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemsControl Grid.Column=\"1\" ItemsSource=\"{Binding DocumentOutline}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Name=\"EditorTextBox\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsSettingsOpen}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportWordCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ExportPdfCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("BatchExportPdfCommand", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ExportPageSize, Mode=TwoWay}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ExportHeaderText, Mode=TwoWay}\"", windowXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModelExposesV0102ShellCoordinatorContracts()
    {
        var viewModelSource = Read("src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs");

        Assert.DoesNotContain("CommandPalette", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("OpenSettingsCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("CheckForUpdatesCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("WorkspaceSearchText", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("WorkspaceHealthIssues", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("WorkspaceHealthSummary", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("WorkspaceStatisticsSummary", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("GitWorkspaceSummary", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("RefreshGitWorkspaceStatusCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportValidationIssues", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportValidationSummary", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ActiveDocumentStatisticsSummary", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("DocumentBacklinks", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("OpenDocumentBacklinkCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("UpdateTableOfContentsCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("InsertTableCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("InsertMermaidDiagramCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("InsertImageCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("DocumentOutline", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportWordCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportPdfCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("BatchExportPdfCommand", viewModelSource, StringComparison.Ordinal);
        Assert.Contains("ExportPageSize", viewModelSource, StringComparison.Ordinal);
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
