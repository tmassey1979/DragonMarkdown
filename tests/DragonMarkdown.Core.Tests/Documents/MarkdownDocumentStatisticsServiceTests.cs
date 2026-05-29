using DragonMarkdown.Core.Documents;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Documents;

public sealed class MarkdownDocumentStatisticsServiceTests
{
    [Fact]
    public void Analyze_CountsDocumentStructureAndReadingTime()
    {
        const string markdown = """
            ---
            title: Sample
            ---
            # Release Notes

            DragonMarkdown edits markdown quickly with live preview.

            ## Images

            ![Logo](logo.png)
            [Website](https://example.test)

            ```csharp
            var ignored = "code";
            ```
            """;

        var statistics = new MarkdownDocumentStatisticsService().Analyze(markdown);

        statistics.WordCount.Should().Be(10);
        statistics.HeadingCount.Should().Be(2);
        statistics.LinkCount.Should().Be(1);
        statistics.ImageCount.Should().Be(1);
        statistics.CodeBlockCount.Should().Be(1);
        statistics.EstimatedReadingMinutes.Should().Be(1);
    }

    [Fact]
    public void Analyze_ReturnsZeroReadingTimeForEmptyMarkdown()
    {
        var statistics = new MarkdownDocumentStatisticsService().Analyze("   ");

        statistics.WordCount.Should().Be(0);
        statistics.EstimatedReadingMinutes.Should().Be(0);
    }
}
