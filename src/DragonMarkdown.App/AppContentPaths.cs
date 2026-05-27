namespace DragonMarkdown.App;

internal static class AppContentPaths
{
    private const string HelpDocumentRelativePath = "Resources/DragonMarkdownHelp.md";

    public static string? FindHelpDocumentPath()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, HelpDocumentRelativePath);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var sourcePath = Path.Combine(directory.FullName, "src", "DragonMarkdown.App", HelpDocumentRelativePath);
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
