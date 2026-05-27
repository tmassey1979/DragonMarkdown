using System.Text;

namespace DragonMarkdown.Core.Exporting;

internal static class MarkdownExportPreprocessor
{
    public static string ReplaceMermaidFencesWithSvg(string markdown)
    {
        var output = new StringBuilder();
        var fence = new StringBuilder();
        var inFence = false;
        var fenceLanguage = string.Empty;

        foreach (var rawLine in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmedLine = rawLine.Trim();

            if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
            {
                if (inFence)
                {
                    AppendFenceOrDiagram(output, fenceLanguage, fence.ToString());
                    fence.Clear();
                    fenceLanguage = string.Empty;
                    inFence = false;
                }
                else
                {
                    inFence = true;
                    fenceLanguage = trimmedLine[3..].Trim();
                }

                continue;
            }

            if (inFence)
            {
                fence.AppendLine(rawLine);
            }
            else
            {
                output.AppendLine(rawLine);
            }
        }

        if (inFence)
        {
            output.AppendLine("```" + fenceLanguage);
            output.Append(fence);
        }

        return output.ToString();
    }

    private static void AppendFenceOrDiagram(StringBuilder output, string language, string source)
    {
        if (string.Equals(language, "mermaid", StringComparison.OrdinalIgnoreCase)
            && MermaidDiagramRenderer.TryRender(source) is { } diagram)
        {
            output.AppendLine();
            output.AppendLine("""<figure class="dragon-mermaid-export">""");
            output.AppendLine(diagram.Svg);
            output.AppendLine("</figure>");
            output.AppendLine();
            return;
        }

        output.AppendLine("```" + language);
        output.Append(source);
        output.AppendLine("```");
    }
}
