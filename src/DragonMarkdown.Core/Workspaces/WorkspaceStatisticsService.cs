using DragonMarkdown.Core.Documents;

namespace DragonMarkdown.Core.Workspaces;

public sealed class WorkspaceStatisticsService
{
    private static readonly ISet<string> SkippedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj",
        "artifacts"
    };

    private readonly MarkdownDocumentStatisticsService documentStatisticsService = new();

    public WorkspaceStatistics Analyze(string workspaceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        string rootFullPath = Path.GetFullPath(workspaceRoot);
        if (!Directory.Exists(rootFullPath))
        {
            throw new DirectoryNotFoundException($"Workspace root was not found: {rootFullPath}");
        }

        var documentCount = 0;
        var wordCount = 0;
        var headingCount = 0;
        var linkCount = 0;
        var imageCount = 0;

        foreach (string filePath in EnumerateMarkdownFiles(rootFullPath))
        {
            documentCount++;
            var statistics = documentStatisticsService.Analyze(File.ReadAllText(filePath));
            wordCount += statistics.WordCount;
            headingCount += statistics.HeadingCount;
            linkCount += statistics.LinkCount;
            imageCount += statistics.ImageCount;
        }

        int readingMinutes = wordCount == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(wordCount / 225.0));
        return new WorkspaceStatistics(documentCount, wordCount, headingCount, linkCount, imageCount, readingMinutes);
    }

    private static IEnumerable<string> EnumerateMarkdownFiles(string directoryPath)
    {
        foreach (string filePath in Directory.EnumerateFiles(directoryPath).Where(IsMarkdownFile))
        {
            yield return filePath;
        }

        foreach (string childDirectory in Directory.EnumerateDirectories(directoryPath).Where(directory => !ShouldSkipFolder(directory)))
        {
            foreach (string filePath in EnumerateMarkdownFiles(childDirectory))
            {
                yield return filePath;
            }
        }
    }

    private static bool IsMarkdownFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mdown", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkipFolder(string directoryPath) =>
        SkippedFolderNames.Contains(Path.GetFileName(directoryPath));
}
