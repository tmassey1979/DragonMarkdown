namespace DragonMarkdown.Core.Workspaces;

public static class WorkspaceTreeBuilder
{
    private static readonly ISet<string> SkippedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        ".vs",
        ".idea",
        "node_modules",
        ".superpowers"
    };

    private static readonly ISet<string> MarkdownExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".mdown"
    };

    private static readonly ISet<string> AssetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".bmp",
        ".webp",
        ".svg",
        ".ico",
        ".csv",
        ".json",
        ".yaml",
        ".yml",
        ".txt",
        ".pdf",
        ".html",
        ".css",
        ".js"
    };

    public static WorkspaceItem Build(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("Workspace root is required.", nameof(workspaceRoot));
        }

        string rootFullPath = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(rootFullPath))
        {
            throw new DirectoryNotFoundException($"Workspace root was not found: {rootFullPath}");
        }

        IReadOnlyList<WorkspaceItem> children = BuildChildren(rootFullPath, rootFullPath);

        return new WorkspaceItem(
            GetDisplayName(rootFullPath),
            rootFullPath,
            string.Empty,
            WorkspaceItemKind.Folder,
            children);
    }

    private static IReadOnlyList<WorkspaceItem> BuildChildren(string directoryPath, string rootFullPath)
    {
        IEnumerable<WorkspaceItem> folders = Directory
            .EnumerateDirectories(directoryPath)
            .Where(directory => !ShouldSkipFolder(directory))
            .Select(directory => BuildFolder(directory, rootFullPath))
            .Where(folder => folder is not null)
            .Cast<WorkspaceItem>()
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal);

        IEnumerable<WorkspaceItem> files = Directory
            .EnumerateFiles(directoryPath)
            .Where(IsIncludedFile)
            .Select(file => BuildFile(file, rootFullPath))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.Ordinal);

        return folders.Concat(files).ToArray();
    }

    private static WorkspaceItem? BuildFolder(string directoryPath, string rootFullPath)
    {
        IReadOnlyList<WorkspaceItem> children = BuildChildren(directoryPath, rootFullPath);
        if (children.Count == 0)
        {
            return null;
        }

        return new WorkspaceItem(
            Path.GetFileName(directoryPath),
            Path.GetFullPath(directoryPath),
            GetRelativePath(rootFullPath, directoryPath),
            WorkspaceItemKind.Folder,
            children);
    }

    private static WorkspaceItem BuildFile(string filePath, string rootFullPath)
    {
        return new WorkspaceItem(
            Path.GetFileName(filePath),
            Path.GetFullPath(filePath),
            GetRelativePath(rootFullPath, filePath),
            GetFileKind(filePath));
    }

    private static WorkspaceItemKind GetFileKind(string filePath)
    {
        if (MarkdownExtensions.Contains(Path.GetExtension(filePath)))
        {
            return WorkspaceItemKind.MarkdownFile;
        }

        return WorkspaceItemKind.AssetFile;
    }

    private static bool ShouldSkipFolder(string directoryPath)
    {
        return SkippedFolderNames.Contains(Path.GetFileName(directoryPath));
    }

    private static bool IsIncludedFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return MarkdownExtensions.Contains(extension) || AssetExtensions.Contains(extension);
    }

    private static string GetDisplayName(string path)
    {
        string name = Path.GetFileName(path);
        return string.IsNullOrEmpty(name) ? path : name;
    }

    private static string GetRelativePath(string rootFullPath, string fullPath)
    {
        string relativePath = Path.GetRelativePath(rootFullPath, fullPath);
        if (relativePath == ".")
        {
            return string.Empty;
        }

        return relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
