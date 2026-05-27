using DragonMarkdown.Core.Rendering;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Rendering;

public sealed class MarkdownOutlineBuilderTests
{
    [Fact]
    public void Build_extracts_headings_from_level_one_through_six()
    {
        const string markdown = """
            # Title
            ## Getting Started
            ### Step 1
            #### Deep Dive
            ##### Details
            ###### Fine Print
            ####### Too Deep
            """;

        var builder = new MarkdownOutlineBuilder();

        IReadOnlyList<MarkdownOutlineItem> outline = builder.Build(markdown);

        outline.Select(item => (item.Level, item.Title, item.Slug)).Should().Equal(
            (1, "Title", "title"),
            (2, "Getting Started", "getting-started"),
            (3, "Step 1", "step-1"),
            (4, "Deep Dive", "deep-dive"),
            (5, "Details", "details"),
            (6, "Fine Print", "fine-print"));
    }

    [Fact]
    public void Build_ignores_hashes_inside_code_fences_and_empty_headings()
    {
        const string markdown = """
            ```
            # Not A Heading
            ```

            ##
            ## Real Heading
            """;

        var builder = new MarkdownOutlineBuilder();

        IReadOnlyList<MarkdownOutlineItem> outline = builder.Build(markdown);

        outline.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MarkdownOutlineItem(2, "Real Heading", "real-heading"));
    }

    [Fact]
    public void Build_adds_suffixes_for_duplicate_slugs()
    {
        const string markdown = """
            # Release Notes
            ## Release Notes!
            """;

        var builder = new MarkdownOutlineBuilder();

        IReadOnlyList<MarkdownOutlineItem> outline = builder.Build(markdown);

        outline.Select(item => item.Slug).Should().Equal("release-notes", "release-notes-1");
    }
}
