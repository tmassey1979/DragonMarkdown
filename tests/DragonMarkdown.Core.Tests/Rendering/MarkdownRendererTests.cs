using DragonMarkdown.Core.Rendering;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Rendering;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void RenderDocument_UsesAdvancedMarkdownExtensions()
    {
        var renderer = new MarkdownRenderer();
        var options = CreateOptions();
        const string markdown = """
            ---
            title: Example
            ---

            | Name | Done |
            | --- | --- |
            | Render | yes |

            - [x] Implement renderer

            Footnote reference.[^preview]

            [^preview]: Footnote body.
            """;

        var result = renderer.RenderDocument(markdown, options);

        result.Html.Should().Contain("<!doctype html>");
        result.Html.Should().Contain("<table>");
        result.Html.Should().Contain("<input disabled=\"disabled\" type=\"checkbox\" checked=\"checked\" />");
        result.Html.Should().Contain("footnotes");
        result.Html.Should().Contain("Footnote body.");
        result.Html.Should().NotContain("title: Example");
    }

    [Fact]
    public void RenderDocument_EmitsScriptHooksForPreviewFeatures()
    {
        var renderer = new MarkdownRenderer();
        var options = CreateOptions();
        const string markdown = """
            ```mermaid
            graph TD
                A --> B
            ```

            ```csharp
            Console.WriteLine("hello");
            ```

            Inline math \(a+b\) and block math:

            \[
            c = d
            \]
            """;

        var result = renderer.RenderDocument(markdown, options);

        result.Html.Should().Contain("class=\"mermaid\"");
        result.Html.Should().Contain("graph TD");
        result.Html.Should().Contain("mermaid.initialize");
        result.Html.Should().Contain("MathJax");
        result.Html.Should().Contain(@"\(a+b\)");
        result.Html.Should().Contain(@"\[");
        result.Html.Should().Contain("Prism.highlightAll");
        result.Html.Should().Contain("language-csharp");
    }

    [Fact]
    public void RenderDocument_RewritesWorkspaceRelativeLinksAndImagesToAppUrls()
    {
        var renderer = new MarkdownRenderer();
        var options = CreateOptions();
        const string markdown = """
            [Guide](docs/guide.md)

            ![Logo](assets/logo.png)
            """;

        var result = renderer.RenderDocument(markdown, options);

        result.Html.Should().Contain("href=\"dragonmarkdown://workspace/docs/guide.md\"");
        result.Html.Should().Contain("src=\"dragonmarkdown://workspace/assets/logo.png\"");
        result.BlockedReferences.Should().BeEmpty();
    }

    [Fact]
    public void RenderDocument_BlocksRelativeReferencesOutsideWorkspace()
    {
        var renderer = new MarkdownRenderer();
        var options = CreateOptions();
        const string markdown = """
            [Secret](../secret.md)

            ![Secret](../outside.png)
            """;

        var result = renderer.RenderDocument(markdown, options);

        result.Html.Should().Contain("href=\"#blocked-local-reference\"");
        result.Html.Should().Contain("src=\"#blocked-local-reference\"");
        result.Html.Should().Contain("data-dragonmarkdown-blocked-reference");
        result.Html.Should().NotContain("secret.md");
        result.Html.Should().NotContain("outside.png");
        result.BlockedReferences.Should().HaveCount(2);
        result.BlockedReferences.Should().OnlyContain(reference => reference.Reason == MarkdownReferenceBlockReason.OutsideWorkspace);
    }

    [Fact]
    public void RenderDocument_BlocksRawLocalFileReferences()
    {
        var renderer = new MarkdownRenderer();
        var options = CreateOptions();
        const string markdown = """
            [Absolute](C:\Users\someone\secret.md)

            ![File](file:///C:/Users/someone/secret.png)
            """;

        var result = renderer.RenderDocument(markdown, options);

        result.Html.Should().Contain("href=\"#blocked-local-reference\"");
        result.Html.Should().Contain("src=\"#blocked-local-reference\"");
        result.Html.Should().NotContain("C:\\Users");
        result.Html.Should().NotContain("file:///C:/Users");
        result.BlockedReferences.Should().HaveCount(2);
        result.BlockedReferences.Should().OnlyContain(reference => reference.Reason == MarkdownReferenceBlockReason.RawLocalPath);
    }

    [Fact]
    public void RenderDocument_PreservesExternalAndAnchorLinks()
    {
        var renderer = new MarkdownRenderer();
        var options = CreateOptions();
        const string markdown = """
            [External](https://example.com/docs)

            [Heading](#heading)
            """;

        var result = renderer.RenderDocument(markdown, options);

        result.Html.Should().Contain("href=\"https://example.com/docs\"");
        result.Html.Should().Contain("href=\"#heading\"");
        result.BlockedReferences.Should().BeEmpty();
    }

    private static MarkdownRenderOptions CreateOptions()
    {
        var workspaceRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DragonMarkdown", "workspace"));
        var documentPath = Path.Combine(workspaceRoot, "README.md");

        return new MarkdownRenderOptions(workspaceRoot, documentPath);
    }
}
