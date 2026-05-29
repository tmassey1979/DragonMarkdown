using DragonMarkdown.Core.Workspaces;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Workspaces;

public sealed class WorkspaceStatisticsServiceTests : IDisposable
{
    private readonly string workspaceRoot;

    public WorkspaceStatisticsServiceTests()
    {
        workspaceRoot = Path.Combine(Path.GetTempPath(), "DragonMarkdown.WorkspaceStats.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
    }

    [Fact]
    public void Analyze_SummarizesMarkdownDocumentsInWorkspace()
    {
        WriteFile("README.md", "# Home\r\nDragonMarkdown tracks the project.");
        WriteFile("docs\\guide.md", "# Guide\r\n## Setup\r\n[Home](../README.md)\r\n![Logo](logo.png)");
        WriteFile("docs\\notes.txt", "not markdown");
        WriteFile("artifacts\\generated.md", "# Ignored");

        var statistics = new WorkspaceStatisticsService().Analyze(workspaceRoot);

        statistics.DocumentCount.Should().Be(2);
        statistics.WordCount.Should().Be(7);
        statistics.HeadingCount.Should().Be(3);
        statistics.LinkCount.Should().Be(1);
        statistics.ImageCount.Should().Be(1);
        statistics.EstimatedReadingMinutes.Should().Be(1);
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
