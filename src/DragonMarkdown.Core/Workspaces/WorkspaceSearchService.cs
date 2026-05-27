namespace DragonMarkdown.Core.Workspaces;

public sealed class WorkspaceSearchService
{
    private static readonly ISet<string> SkippedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        "artifacts"
    };

    private static readonly ISet<string> SearchableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".mdown",
        ".txt"
    };

    public IReadOnlyList<WorkspaceSearchResult> Search(string workspaceRoot, string query)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        string rootFullPath = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(rootFullPath))
        {
            throw new DirectoryNotFoundException($"Workspace root was not found: {rootFullPath}");
        }

        string trimmedQuery = query.Trim();

        return EnumerateSearchableFiles(rootFullPath)
            .Select(filePath => BuildResult(rootFullPath, filePath, trimmedQuery))
            .Where(result => result is not null)
            .Cast<WorkspaceSearchResult>()
            .OrderBy(result => result.MatchKind)
            .ThenBy(result => result.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateSearchableFiles(string directoryPath)
    {
        foreach (string filePath in Directory.EnumerateFiles(directoryPath).Where(IsSearchableFile))
        {
            yield return filePath;
        }

        foreach (string childDirectory in Directory.EnumerateDirectories(directoryPath).Where(directory => !ShouldSkipFolder(directory)))
        {
            foreach (string filePath in EnumerateSearchableFiles(childDirectory))
            {
                yield return filePath;
            }
        }
    }

    private static WorkspaceSearchResult? BuildResult(string rootFullPath, string filePath, string query)
    {
        string relativePath = GetRelativePath(rootFullPath, filePath);
        string content = File.ReadAllText(filePath);
        string title = GetTitle(filePath, content);

        WorkspaceSearchMatchKind? matchKind = GetMatchKind(relativePath, title, content, query);
        if (matchKind is null)
        {
            return null;
        }

        return new WorkspaceSearchResult(
            Path.GetFullPath(filePath),
            relativePath,
            title,
            matchKind.Value,
            GetPreview(content, query));
    }

    private static WorkspaceSearchMatchKind? GetMatchKind(string relativePath, string title, string content, string query)
    {
        if (Contains(relativePath, query))
        {
            return WorkspaceSearchMatchKind.Path;
        }

        if (Contains(title, query))
        {
            return WorkspaceSearchMatchKind.Title;
        }

        if (Contains(content, query))
        {
            return WorkspaceSearchMatchKind.Content;
        }

        return null;
    }

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

    private static string GetPreview(string content, string query)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (Contains(line, query))
            {
                return line.Trim();
            }
        }

        return string.Empty;
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool ShouldSkipFolder(string directoryPath) =>
        SkippedFolderNames.Contains(Path.GetFileName(directoryPath));

    private static bool IsSearchableFile(string filePath) =>
        SearchableExtensions.Contains(Path.GetExtension(filePath));

    private static string GetRelativePath(string rootFullPath, string fullPath) =>
        Path.GetRelativePath(rootFullPath, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
}
