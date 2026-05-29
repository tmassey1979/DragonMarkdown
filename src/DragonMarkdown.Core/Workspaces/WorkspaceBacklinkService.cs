using System.Text.RegularExpressions;

namespace DragonMarkdown.Core.Workspaces;

public sealed partial class WorkspaceBacklinkService
{
    private static readonly ISet<string> SkippedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        "artifacts"
    };

    public IReadOnlyList<WorkspaceBacklink> FindBacklinks(string workspaceRoot, string targetDocumentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDocumentPath);

        string rootFullPath = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(rootFullPath))
        {
            throw new DirectoryNotFoundException($"Workspace root was not found: {rootFullPath}");
        }

        string targetFullPath = Path.GetFullPath(targetDocumentPath);

        return EnumerateMarkdownFiles(rootFullPath)
            .Where(sourcePath => !sourcePath.Equals(targetFullPath, StringComparison.OrdinalIgnoreCase))
            .Select(sourcePath => BuildBacklink(rootFullPath, sourcePath, targetFullPath))
            .Where(backlink => backlink is not null)
            .Cast<WorkspaceBacklink>()
            .OrderBy(backlink => GetPathDepth(backlink.RelativePath))
            .ThenBy(backlink => backlink.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(backlink => backlink.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateMarkdownFiles(string directoryPath)
    {
        foreach (string filePath in Directory.EnumerateFiles(directoryPath).Where(IsMarkdownFile))
        {
            yield return Path.GetFullPath(filePath);
        }

        foreach (string childDirectory in Directory.EnumerateDirectories(directoryPath).Where(directory => !ShouldSkipFolder(directory)))
        {
            foreach (string filePath in EnumerateMarkdownFiles(childDirectory))
            {
                yield return filePath;
            }
        }
    }

    private static WorkspaceBacklink? BuildBacklink(string rootFullPath, string sourcePath, string targetFullPath)
    {
        string markdown = File.ReadAllText(sourcePath);
        foreach (Match match in LinkRegex().Matches(markdown))
        {
            string reference = NormalizeMarkdownReference(match.Groups["path"].Value);
            if (IsExternalReference(reference) || string.IsNullOrWhiteSpace(reference))
            {
                continue;
            }

            string resolvedPath = ResolveReference(sourcePath, reference);
            if (!resolvedPath.Equals(targetFullPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new WorkspaceBacklink(
                sourcePath,
                ToRelativePath(rootFullPath, sourcePath),
                GetTitle(sourcePath, markdown),
                GetPreviewLine(markdown, match.Index));
        }

        return null;
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

    private static string ResolveReference(string sourcePath, string reference) =>
        Path.IsPathFullyQualified(reference)
            ? Path.GetFullPath(reference)
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory, reference));

    private static string GetTitle(string filePath, string content)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                return trimmed[2..].Trim();
            }
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    private static string GetPreviewLine(string markdown, int matchIndex)
    {
        int lineStart = markdown.LastIndexOf('\n', Math.Max(0, matchIndex - 1));
        int lineEnd = markdown.IndexOf('\n', matchIndex);
        int start = lineStart < 0 ? 0 : lineStart + 1;
        int end = lineEnd < 0 ? markdown.Length : lineEnd;
        return markdown[start..end].Trim();
    }

    private static bool IsExternalReference(string reference) =>
        Uri.TryCreate(reference, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == "data"
            || uri.Scheme == "mailto");

    private static bool IsMarkdownFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mdown", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkipFolder(string directoryPath) =>
        SkippedFolderNames.Contains(Path.GetFileName(directoryPath));

    private static int GetPathDepth(string relativePath) =>
        relativePath.Count(character => character == '/');

    private static string ToRelativePath(string rootFullPath, string fullPath) =>
        Path.GetRelativePath(rootFullPath, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');

    [GeneratedRegex(@"(?<!!)\[[^\]]+\]\((?<path>[^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();
}
