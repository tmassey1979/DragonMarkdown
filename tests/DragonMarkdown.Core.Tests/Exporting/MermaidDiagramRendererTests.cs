using DragonMarkdown.Core.Exporting;
using FluentAssertions;

namespace DragonMarkdown.Core.Tests.Exporting;

public sealed class MermaidDiagramRendererTests
{
    [Fact]
    public void TryRender_RendersFlowchartEdgesAsSvg()
    {
        const string source = """
            graph TD
                A[Open file] --> B[Render preview]
                B --> C[Export]
            """;

        var rendered = MermaidDiagramRenderer.TryRender(source);

        rendered.Should().NotBeNull();
        rendered!.Svg.Should().Contain("<svg");
        rendered.Svg.Should().Contain("dragon-mermaid-diagram");
        rendered.Svg.Should().Contain("Open file");
        rendered.Svg.Should().Contain("Render preview");
        rendered.Svg.Should().Contain("Export");
        rendered.Svg.Should().Contain("<line");
    }

    [Fact]
    public void TryRender_ReturnsNullForUnsupportedMermaidSyntax()
    {
        const string source = """
            sequenceDiagram
                participant A
                participant B
                A->>B: Hello
            """;

        MermaidDiagramRenderer.TryRender(source).Should().BeNull();
    }
}
