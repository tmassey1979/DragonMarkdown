using DragonMarkdown.Core.Documents;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Documents;

public sealed class MarkdownTableOfContentsServiceTests
{
    [Fact]
    public void UpdateTableOfContents_InsertsMarkedTocBeforeContent()
    {
        const string markdown = """
            # Guide

            ## Setup

            ### Install
            """;

        string updated = new MarkdownTableOfContentsService().UpdateTableOfContents(markdown);

        updated.Should().StartWith("""
            <!-- DragonMarkdown TOC -->
            - [Guide](#guide)
              - [Setup](#setup)
                - [Install](#install)
            <!-- /DragonMarkdown TOC -->

            # Guide
            """.Replace("\r\n", "\n"));
    }

    [Fact]
    public void UpdateTableOfContents_ReplacesExistingMarkedToc()
    {
        const string markdown = """
            <!-- DragonMarkdown TOC -->
            - [Old](#old)
            <!-- /DragonMarkdown TOC -->

            # New
            """;

        string updated = new MarkdownTableOfContentsService().UpdateTableOfContents(markdown);

        updated.Should().Contain("- [New](#new)");
        updated.Should().NotContain("- [Old](#old)");
        updated.Split("<!-- DragonMarkdown TOC -->").Length.Should().Be(2);
    }

    [Fact]
    public void UpdateTableOfContents_ReturnsOriginalMarkdownWhenNoHeadingsExist()
    {
        const string markdown = "Plain text only.";

        string updated = new MarkdownTableOfContentsService().UpdateTableOfContents(markdown);

        updated.Should().Be(markdown);
    }
}
