using DragonMarkdown.Core.Health;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Health;

public sealed class WorkspaceHealthAnalyzerTests : IDisposable
{
    private readonly string workspaceRoot;

    public WorkspaceHealthAnalyzerTests()
    {
        workspaceRoot = Path.Combine(Path.GetTempPath(), "DragonMarkdown.Health.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
    }

    [Fact]
    public void Analyze_reports_missing_images_and_broken_markdown_links()
    {
        string readme = Path.Combine(workspaceRoot, "README.md");
        File.WriteAllText(readme, """
            # README

            ![Missing](images/missing.png)
            [Missing Doc](docs/missing.md)
            [External](https://example.test)
            """);
        var analyzer = new WorkspaceHealthAnalyzer();

        WorkspaceHealthReport report = analyzer.Analyze(workspaceRoot);

        report.Issues.Should().Contain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.MissingImage
            && issue.DocumentPath == readme
            && issue.Reference == "images/missing.png");
        report.Issues.Should().Contain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.BrokenLink
            && issue.DocumentPath == readme
            && issue.Reference == "docs/missing.md");
        report.Issues.Should().NotContain(issue => issue.Reference == "https://example.test");
    }

    [Fact]
    public void Analyze_reports_duplicate_headings_within_a_document()
    {
        string readme = Path.Combine(workspaceRoot, "README.md");
        File.WriteAllText(readme, """
            # Install

            ## Usage

            ## Usage
            """);
        var analyzer = new WorkspaceHealthAnalyzer();

        WorkspaceHealthReport report = analyzer.Analyze(workspaceRoot);

        report.Issues.Should().Contain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.DuplicateHeading
            && issue.DocumentPath == readme
            && issue.Message.Contains("Usage", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_reports_malformed_front_matter_and_unsupported_mermaid()
    {
        string readme = Path.Combine(workspaceRoot, "README.md");
        File.WriteAllText(readme, """
            ---
            title: Bad Front Matter

            # Diagram

            ```mermaid
            sequenceDiagram
              Alice->>Bob: Hello
            ```
            """);
        var analyzer = new WorkspaceHealthAnalyzer();

        WorkspaceHealthReport report = analyzer.Analyze(workspaceRoot);

        report.Issues.Should().Contain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.MalformedFrontMatter
            && issue.DocumentPath == readme);
        report.Issues.Should().Contain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.UnsupportedMermaid
            && issue.DocumentPath == readme);
    }

    [Fact]
    public void Analyze_reports_orphan_markdown_documents()
    {
        string readme = Path.Combine(workspaceRoot, "README.md");
        string linked = Path.Combine(workspaceRoot, "linked.md");
        string orphan = Path.Combine(workspaceRoot, "orphan.md");
        File.WriteAllText(readme, "# Home" + Environment.NewLine + "[Linked](linked.md)");
        File.WriteAllText(linked, "# Linked");
        File.WriteAllText(orphan, "# Orphan");
        var analyzer = new WorkspaceHealthAnalyzer();

        WorkspaceHealthReport report = analyzer.Analyze(workspaceRoot);

        report.Issues.Should().Contain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.OrphanDocument
            && issue.DocumentPath == orphan);
        report.Issues.Should().NotContain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.OrphanDocument
            && issue.DocumentPath == linked);
        report.Issues.Should().NotContain(issue =>
            issue.Code == WorkspaceHealthIssueCodes.OrphanDocument
            && issue.DocumentPath == readme);
    }

    public void Dispose()
    {
        if (Directory.Exists(workspaceRoot))
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }
}
