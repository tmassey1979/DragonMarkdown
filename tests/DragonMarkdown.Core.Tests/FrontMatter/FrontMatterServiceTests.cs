using DragonMarkdown.Core.FrontMatter;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.FrontMatter;

public sealed class FrontMatterServiceTests
{
    [Fact]
    public void Parse_ReturnsMetadataAndBodyWhenDocumentStartsWithFrontMatter()
    {
        const string markdown = """
            ---
            title: Release Notes
            author: Dragon Team
            draft: false
            ---
            # Release Notes

            Body text.
            """;

        var result = FrontMatterService.Parse(markdown);

        result.Metadata.Should().Contain(new KeyValuePair<string, string>("title", "Release Notes"));
        result.Metadata.Should().Contain(new KeyValuePair<string, string>("author", "Dragon Team"));
        result.Metadata.Should().Contain(new KeyValuePair<string, string>("draft", "false"));
        result.Body.Should().Be("# Release Notes\n\nBody text.");
    }

    [Fact]
    public void Parse_ReturnsOriginalBodyWhenFrontMatterIsMissing()
    {
        const string markdown = """
            # Notes

            Body text.
            """;

        var result = FrontMatterService.Parse(markdown);

        result.Metadata.Should().BeEmpty();
        result.Body.Should().Be(markdown);
    }
}
