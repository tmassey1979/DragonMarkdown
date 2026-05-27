using DragonMarkdown.Core.Workspaces;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Workspaces;

public sealed class WorkspaceSearchServiceTests : IDisposable
{
    private readonly string workspaceRoot;

    public WorkspaceSearchServiceTests()
    {
        workspaceRoot = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Search.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
    }

    [Fact]
    public void Search_returns_markdown_and_text_matches_ranked_by_path_title_then_content()
    {
        WriteFile("alpha-target.md", "# Notes\r\nbody without query");
        WriteFile("docs\\guide.md", "# Target Guide\r\nbody without query");
        WriteFile("docs\\journal.txt", "plain text mentions target here");
        WriteFile("docs\\ignored.json", "target");

        var service = new WorkspaceSearchService();

        IReadOnlyList<WorkspaceSearchResult> results = service.Search(workspaceRoot, "target");

        results.Select(result => (result.RelativePath, result.MatchKind)).Should().Equal(
            ("alpha-target.md", WorkspaceSearchMatchKind.Path),
            ("docs/guide.md", WorkspaceSearchMatchKind.Title),
            ("docs/journal.txt", WorkspaceSearchMatchKind.Content));
    }

    [Fact]
    public void Search_skips_generated_and_internal_folders()
    {
        WriteFile("README.md", "# Target");
        WriteFile(".git\\tracked.md", "# Target");
        WriteFile("bin\\debug.md", "# Target");
        WriteFile("obj\\generated.txt", "target");
        WriteFile("artifacts\\report.md", "# Target");

        var service = new WorkspaceSearchService();

        IReadOnlyList<WorkspaceSearchResult> results = service.Search(workspaceRoot, "target");

        results.Select(result => result.RelativePath).Should().Equal("README.md");
    }

    [Fact]
    public void Search_returns_empty_results_for_blank_query()
    {
        WriteFile("README.md", "# Target");
        var service = new WorkspaceSearchService();

        IReadOnlyList<WorkspaceSearchResult> results = service.Search(workspaceRoot, " ");

        results.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(workspaceRoot))
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }

    private void WriteFile(string relativePath, string content)
    {
        string[] pathSegments = relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        string fullPath = Path.Combine([workspaceRoot, .. pathSegments]);
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }
}
