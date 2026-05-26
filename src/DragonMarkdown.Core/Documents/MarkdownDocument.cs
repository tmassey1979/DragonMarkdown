namespace DragonMarkdown.Core.Documents;

public sealed class MarkdownDocument
{
    private MarkdownDocument(string filePath, string text)
    {
        FilePath = filePath;
        DisplayName = GetDisplayName(filePath);
        Text = text;
        OriginalText = text;
    }

    public string FilePath { get; }

    public string DisplayName { get; }

    public string Text { get; private set; }

    public string OriginalText { get; private set; }

    public bool IsDirty => !string.Equals(Text, OriginalText, StringComparison.Ordinal);

    internal static MarkdownDocument Open(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);
        return new MarkdownDocument(fullPath, File.ReadAllText(fullPath));
    }

    public void UpdateText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
    }

    public void Save()
    {
        File.WriteAllText(FilePath, Text);
        OriginalText = Text;
    }

    private static string GetDisplayName(string filePath)
    {
        string displayName = Path.GetFileName(filePath);
        return string.IsNullOrEmpty(displayName) ? filePath : displayName;
    }
}
