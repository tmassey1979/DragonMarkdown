using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DragonMarkdown.Core.Exporting;

public static partial class MermaidDiagramRenderer
{
    private const int NodeWidth = 150;
    private const int NodeHeight = 52;
    private const int HorizontalGap = 84;
    private const int VerticalGap = 62;

    public static MermaidRenderedDiagram? TryRender(string mermaidSource)
    {
        ArgumentNullException.ThrowIfNull(mermaidSource);

        var lines = mermaidSource
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("%%", StringComparison.Ordinal))
            .ToArray();

        if (lines.Length == 0)
        {
            return null;
        }

        var headerMatch = HeaderRegex().Match(lines[0]);
        if (!headerMatch.Success)
        {
            return null;
        }

        var direction = headerMatch.Groups["direction"].Value.ToUpperInvariant();
        if (direction is "RL" or "BT")
        {
            return null;
        }

        var nodes = new Dictionary<string, MermaidNode>(StringComparer.Ordinal);
        var nodeOrder = new List<string>();
        var edges = new List<MermaidEdge>();

        foreach (var line in lines.Skip(1))
        {
            var nodeMatches = NodeRegex().Matches(RemoveEdgeLabels(line));
            if (nodeMatches.Count < 2)
            {
                continue;
            }

            var from = AddNode(nodeMatches[0], nodes, nodeOrder);
            var to = AddNode(nodeMatches[1], nodes, nodeOrder);
            edges.Add(new MermaidEdge(from, to));
        }

        if (nodes.Count == 0 || edges.Count == 0)
        {
            return null;
        }

        var leftToRight = direction == "LR";
        var width = leftToRight
            ? 48 + nodeOrder.Count * NodeWidth + (nodeOrder.Count - 1) * HorizontalGap
            : NodeWidth + 96;
        var height = leftToRight
            ? NodeHeight + 96
            : 48 + nodeOrder.Count * NodeHeight + (nodeOrder.Count - 1) * VerticalGap;

        var positions = nodeOrder
            .Select((id, index) => new
            {
                Id = id,
                X = leftToRight ? 24 + index * (NodeWidth + HorizontalGap) : 48,
                Y = leftToRight ? 48 : 24 + index * (NodeHeight + VerticalGap)
            })
            .ToDictionary(item => item.Id, item => new DiagramPoint(item.X, item.Y), StringComparer.Ordinal);

        var svg = BuildSvg(nodes, nodeOrder, edges, positions, width, height, leftToRight);
        return new MermaidRenderedDiagram(svg, width, height);
    }

    private static string AddNode(
        Match match,
        IDictionary<string, MermaidNode> nodes,
        ICollection<string> nodeOrder)
    {
        var id = match.Groups["id"].Value;
        var label = GetNodeLabel(match, id);

        if (!nodes.ContainsKey(id))
        {
            nodes[id] = new MermaidNode(id, label);
            nodeOrder.Add(id);
        }
        else if (string.Equals(nodes[id].Label, id, StringComparison.Ordinal) && !string.Equals(label, id, StringComparison.Ordinal))
        {
            nodes[id] = nodes[id] with { Label = label };
        }

        return id;
    }

    private static string GetNodeLabel(Match match, string id)
    {
        foreach (var groupName in new[] { "square", "round", "brace" })
        {
            if (match.Groups[groupName].Success)
            {
                return match.Groups[groupName].Value.Trim();
            }
        }

        return id;
    }

    private static string RemoveEdgeLabels(string line) =>
        EdgeLabelRegex().Replace(line, " ");

    private static string BuildSvg(
        IReadOnlyDictionary<string, MermaidNode> nodes,
        IReadOnlyList<string> nodeOrder,
        IReadOnlyList<MermaidEdge> edges,
        IReadOnlyDictionary<string, DiagramPoint> positions,
        int width,
        int height,
        bool leftToRight)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" class="dragon-mermaid-diagram" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img">""");
        builder.AppendLine("""
            <defs>
              <marker id="dragon-mermaid-arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto" markerUnits="strokeWidth">
                <path d="M0,0 L0,6 L9,3 z" fill="#3E5C76" />
              </marker>
            </defs>
            <rect width="100%" height="100%" rx="10" fill="#F7FAFC" />
            """);

        foreach (var edge in edges)
        {
            var from = positions[edge.From];
            var to = positions[edge.To];
            var x1 = leftToRight ? from.X + NodeWidth : from.X + NodeWidth / 2;
            var y1 = leftToRight ? from.Y + NodeHeight / 2 : from.Y + NodeHeight;
            var x2 = leftToRight ? to.X : to.X + NodeWidth / 2;
            var y2 = leftToRight ? to.Y + NodeHeight / 2 : to.Y;

            builder.AppendLine($"""  <line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#3E5C76" stroke-width="2.5" marker-end="url(#dragon-mermaid-arrow)" />""");
        }

        foreach (var nodeId in nodeOrder)
        {
            var node = nodes[nodeId];
            var position = positions[nodeId];
            builder.AppendLine($"""  <rect x="{position.X}" y="{position.Y}" width="{NodeWidth}" height="{NodeHeight}" rx="8" fill="#FFFFFF" stroke="#9AB0C5" stroke-width="1.5" />""");
            builder.AppendLine($"""  <text x="{position.X + NodeWidth / 2}" y="{position.Y + NodeHeight / 2 + 5}" text-anchor="middle" font-family="Inter, Segoe UI, sans-serif" font-size="13" fill="#253041">{WebUtility.HtmlEncode(node.Label)}</text>""");
        }

        builder.AppendLine("</svg>");
        return builder.ToString();
    }

    [GeneratedRegex(@"^(graph|flowchart)\s+(?<direction>TD|TB|LR|RL|BT)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"(?<id>[A-Za-z][A-Za-z0-9_]*)(?:\[(?<square>[^\]]+)\]|\((?<round>[^)]+)\)|\{(?<brace>[^}]+)\})?", RegexOptions.CultureInvariant)]
    private static partial Regex NodeRegex();

    [GeneratedRegex(@"\|[^|]+\|", RegexOptions.CultureInvariant)]
    private static partial Regex EdgeLabelRegex();

    private sealed record MermaidNode(string Id, string Label);

    private sealed record MermaidEdge(string From, string To);

    private sealed record DiagramPoint(int X, int Y);
}
