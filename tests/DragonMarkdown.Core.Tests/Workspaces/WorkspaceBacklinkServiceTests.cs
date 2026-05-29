using DragonMarkdown.Core.Workspaces;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Workspaces;

public sealed class WorkspaceBacklinkServiceTests : IDisposable
{
    private readonly string workspaceRoot;

    public WorkspaceBacklinkServiceTests()
    {
        workspaceRoot = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Backlink.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
    }

    [Fact]
    public void FindBacklinks_ReturnsMarkdownDocumentsThatLinkToTarget()
    {
        WriteFile("README.md", "# Home\r\n[Guide](docs/guide.md)");
        WriteFile("docs\\index.md", "# Index\r\nSee [the guide](guide.md#intro).");
        WriteFile("docs\\guide.md", "# Guide");
        WriteFile("docs\\ignored.txt", "[Guide](guide.md)");
        WriteFile("docs\\unrelated.md", "# Other");

        var service = new WorkspaceBacklinkService();

        IReadOnlyList<WorkspaceBacklink> backlinks = service.FindBacklinks(
            workspaceRoot,
            Path.Combine(workspaceRoot, "docs", "guide.md"));

        backlinks.Select(backlink => (backlink.RelativePath, backlink.Title, backlink.Preview)).Should().Equal(
            ("README.md", "Home", "[Guide](docs/guide.md)"),
            ("docs/index.md", "Index", "See [the guide](guide.md#intro)."));
    }

    [Fact]
    public void FindBacklinks_IgnoresSelfLinksAndGeneratedFolders()
    {
        WriteFile("README.md", "# Home\r\n[Self](README.md)");
        WriteFile("docs\\guide.md", "# Guide\r\n[Home](../README.md)");
        WriteFile("artifacts\\generated.md", "[Home](../README.md)");

        var service = new WorkspaceBacklinkService();

        IReadOnlyList<WorkspaceBacklink> backlinks = service.FindBacklinks(
            workspaceRoot,
            Path.Combine(workspaceRoot, "README.md"));

        backlinks.Select(backlink => backlink.RelativePath).Should().Equal("docs/guide.md");
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
