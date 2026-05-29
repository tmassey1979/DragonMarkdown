using System.Text.RegularExpressions;
using DragonMarkdown.Core.Exporting;
using DragonMarkdown.Core.Rendering;

namespace DragonMarkdown.Core.Health;

public sealed partial class WorkspaceHealthAnalyzer
{
    private readonly MarkdownOutlineBuilder outlineBuilder = new();

    public WorkspaceHealthReport Analyze(string workspaceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        string root = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Workspace does not exist: {root}");
        }

        var markdownFiles = Directory
            .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(IsMarkdownFile)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var issues = new List<WorkspaceHealthIssue>();
        var linkedDocuments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string documentPath in markdownFiles)
        {
            string markdown = File.ReadAllText(documentPath);
            AnalyzeDocument(documentPath, markdown, linkedDocuments, issues);
        }

        AddOrphanDocumentIssues(markdownFiles, linkedDocuments, issues);
        return new WorkspaceHealthReport(issues);
    }

    private void AnalyzeDocument(
        string documentPath,
        string markdown,
        ISet<string> linkedDocuments,
        ICollection<WorkspaceHealthIssue> issues)
    {
        AddReferenceIssues(documentPath, markdown, linkedDocuments, issues);
        AddDuplicateHeadingIssues(documentPath, markdown, issues);
        AddFrontMatterIssues(documentPath, markdown, issues);
        AddMermaidIssues(documentPath, markdown, issues);
    }

    private static void AddReferenceIssues(
        string documentPath,
        string markdown,
        ISet<string> linkedDocuments,
        ICollection<WorkspaceHealthIssue> issues)
    {
        foreach (Match match in ImageRegex().Matches(markdown))
        {
            string reference = NormalizeMarkdownReference(match.Groups["path"].Value);
            if (IsExternalReference(reference) || IsAnchorReference(reference))
            {
                continue;
            }

            string resolvedPath = ResolveReference(documentPath, reference);
            if (!File.Exists(resolvedPath))
            {
                issues.Add(new WorkspaceHealthIssue(
                    WorkspaceHealthIssueCodes.MissingImage,
                    WorkspaceHealthIssueSeverity.Error,
                    $"Local image does not exist: {reference}",
                    documentPath,
                    reference,
                    GetLineNumber(markdown, match.Index)));
            }
        }

        foreach (Match match in LinkRegex().Matches(markdown))
        {
            string reference = NormalizeMarkdownReference(match.Groups["path"].Value);
            if (IsExternalReference(reference) || IsAnchorReference(reference))
            {
                continue;
            }

            string resolvedPath = ResolveReference(documentPath, reference);
            if (IsMarkdownFile(resolvedPath))
            {
                linkedDocuments.Add(Path.GetFullPath(resolvedPath));
            }

            if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            {
                issues.Add(new WorkspaceHealthIssue(
                    WorkspaceHealthIssueCodes.BrokenLink,
                    WorkspaceHealthIssueSeverity.Error,
                    $"Local link target does not exist: {reference}",
                    documentPath,
                    reference,
                    GetLineNumber(markdown, match.Index)));
            }
        }
    }

    private void AddDuplicateHeadingIssues(
        string documentPath,
        string markdown,
        ICollection<WorkspaceHealthIssue> issues)
    {
        var duplicateGroups = outlineBuilder
            .Build(markdown)
            .GroupBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            issues.Add(new WorkspaceHealthIssue(
                WorkspaceHealthIssueCodes.DuplicateHeading,
                WorkspaceHealthIssueSeverity.Warning,
                $"Heading appears more than once: {group.Key}",
                documentPath,
                group.Key));
        }
    }

    private static void AddFrontMatterIssues(
        string documentPath,
        string markdown,
        ICollection<WorkspaceHealthIssue> issues)
    {
        string normalized = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return;
        }

        if (normalized.IndexOf("\n---\n", 4, StringComparison.Ordinal) < 0)
        {
            issues.Add(new WorkspaceHealthIssue(
                WorkspaceHealthIssueCodes.MalformedFrontMatter,
                WorkspaceHealthIssueSeverity.Warning,
                "Front matter starts with --- but does not include a closing delimiter.",
                documentPath));
        }
    }

    private static void AddMermaidIssues(
        string documentPath,
        string markdown,
        ICollection<WorkspaceHealthIssue> issues)
    {
        foreach (Match match in MermaidFenceRegex().Matches(markdown))
        {
            string source = match.Groups["source"].Value.Trim();
            if (MermaidDiagramRenderer.TryRender(source) is null)
            {
                issues.Add(new WorkspaceHealthIssue(
                    WorkspaceHealthIssueCodes.UnsupportedMermaid,
                    WorkspaceHealthIssueSeverity.Warning,
                    "Mermaid diagram syntax is not supported by the built-in exporter.",
                    documentPath,
                    source,
                    GetLineNumber(markdown, match.Index)));
            }
        }
    }

    private static void AddOrphanDocumentIssues(
        IReadOnlyList<string> markdownFiles,
        ISet<string> linkedDocuments,
        ICollection<WorkspaceHealthIssue> issues)
    {
        foreach (string documentPath in markdownFiles)
        {
            string fileName = Path.GetFileName(documentPath);
            if (fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase)
                || linkedDocuments.Contains(documentPath))
            {
                continue;
            }

            issues.Add(new WorkspaceHealthIssue(
                WorkspaceHealthIssueCodes.OrphanDocument,
                WorkspaceHealthIssueSeverity.Info,
                "Markdown document is not linked from another document.",
                documentPath));
        }
    }

    private static string NormalizeMarkdownReference(string reference)
    {
        string withoutTitle = reference.Trim();
        int titleIndex = withoutTitle.IndexOf(" \"", StringComparison.Ordinal);
        if (titleIndex > 0)
        {
            withoutTitle = withoutTitle[..titleIndex];
        }

        int anchorIndex = withoutTitle.IndexOf('#', StringComparison.Ordinal);
        return anchorIndex > 0 ? withoutTitle[..anchorIndex] : withoutTitle;
    }

    private static string ResolveReference(string documentPath, string reference)
    {
        return Path.IsPathFullyQualified(reference)
            ? Path.GetFullPath(reference)
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(documentPath) ?? Environment.CurrentDirectory, reference));
    }

    private static bool IsExternalReference(string reference) =>
        Uri.TryCreate(reference, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == "data"
            || uri.Scheme == "mailto");

    private static bool IsAnchorReference(string reference) =>
        reference.StartsWith('#') || string.IsNullOrWhiteSpace(reference);

    private static bool IsMarkdownFile(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mdown", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetLineNumber(string markdown, int matchIndex) =>
        markdown[..Math.Min(matchIndex, markdown.Length)].Count(character => character == '\n') + 1;

    [GeneratedRegex(@"!\[[^\]]*\]\((?<path>[^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"(?<!!)\[[^\]]+\]\((?<path>[^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"```mermaid\s*(?<source>.*?)```", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex MermaidFenceRegex();
}
