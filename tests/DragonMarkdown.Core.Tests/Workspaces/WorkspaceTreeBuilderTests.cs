using DragonMarkdown.Core.Workspaces;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Workspaces;

public sealed class WorkspaceTreeBuilderTests : IDisposable
{
    private readonly string _workspaceRoot;

    public WorkspaceTreeBuilderTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "DragonMarkdownTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRoot);
    }

    [Fact]
    public void Build_includes_markdown_files_and_supporting_assets()
    {
        WriteFile("README.md");
        WriteFile("docs\\guide.markdown");
        WriteFile("docs\\notes.mdown");
        WriteFile("assets\\logo.png");
        WriteFile("assets\\data.csv");
        WriteFile("assets\\settings.json");
        WriteFile("assets\\frontmatter.yaml");
        WriteFile("assets\\frontmatter.yml");
        WriteFile("assets\\notes.txt");
        WriteFile("assets\\manual.pdf");
        WriteFile("site\\index.html");
        WriteFile("site\\styles.css");
        WriteFile("site\\app.js");
        WriteFile("ignored\\program.cs");

        WorkspaceItem tree = WorkspaceTreeBuilder.Build(_workspaceRoot);

        tree.Kind.Should().Be(WorkspaceItemKind.Folder);
        tree.Name.Should().Be(Path.GetFileName(_workspaceRoot));
        tree.FullPath.Should().Be(Path.GetFullPath(_workspaceRoot));
        tree.RelativePath.Should().BeEmpty();
        FlattenRelativePaths(tree).Should().Equal(
            "assets",
            "assets/data.csv",
            "assets/frontmatter.yaml",
            "assets/frontmatter.yml",
            "assets/logo.png",
            "assets/manual.pdf",
            "assets/notes.txt",
            "assets/settings.json",
            "docs",
            "docs/guide.markdown",
            "docs/notes.mdown",
            "site",
            "site/app.js",
            "site/index.html",
            "site/styles.css",
            "README.md");
    }

    [Fact]
    public void Build_skips_generated_and_internal_folders()
    {
        WriteFile("README.md");
        WriteFile(".git\\tracked.md");
        WriteFile("bin\\debug.md");
        WriteFile("obj\\generated.md");
        WriteFile(".vs\\state.md");
        WriteFile(".idea\\project.md");
        WriteFile("node_modules\\package\\README.md");
        WriteFile(".superpowers\\notes.md");

        WorkspaceItem tree = WorkspaceTreeBuilder.Build(_workspaceRoot);

        FlattenRelativePaths(tree).Should().Equal("README.md");
    }

    [Fact]
    public void Build_orders_folders_before_files_then_alphabetically()
    {
        WriteFile("zeta.md");
        WriteFile("Alpha.md");
        WriteFile("content\\b.md");
        WriteFile("assets\\z.png");
        WriteFile("assets\\a.png");

        WorkspaceItem tree = WorkspaceTreeBuilder.Build(_workspaceRoot);

        tree.Children.Select(child => child.RelativePath).Should().Equal(
            "assets",
            "content",
            "Alpha.md",
            "zeta.md");

        tree.Children[0].Children.Select(child => child.RelativePath).Should().Equal(
            "assets/a.png",
            "assets/z.png");
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    private void WriteFile(string relativePath)
    {
        string[] pathSegments = relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        string fullPath = Path.Combine([_workspaceRoot, .. pathSegments]);
        string? directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, string.Empty);
    }

    private static IReadOnlyList<string> FlattenRelativePaths(WorkspaceItem root)
    {
        List<string> paths = [];
        AddChildren(root, paths);
        return paths;
    }

    private static void AddChildren(WorkspaceItem item, List<string> paths)
    {
        foreach (WorkspaceItem child in item.Children)
        {
            paths.Add(child.RelativePath);
            AddChildren(child, paths);
        }
    }
}
