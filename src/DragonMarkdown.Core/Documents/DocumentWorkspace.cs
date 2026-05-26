namespace DragonMarkdown.Core.Documents;

public sealed class DocumentWorkspace
{
    private static readonly StringComparer FilePathComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private readonly List<MarkdownDocument> documents = [];

    public IReadOnlyList<MarkdownDocument> Documents => documents.AsReadOnly();

    public MarkdownDocument? ActiveDocument { get; private set; }

    public MarkdownDocument OpenDocument(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Document file path is required.", nameof(filePath));
        }

        string fullPath = Path.GetFullPath(filePath);
        MarkdownDocument? existingDocument = documents.FirstOrDefault(document =>
            FilePathComparer.Equals(document.FilePath, fullPath));

        if (existingDocument is not null)
        {
            ActiveDocument = existingDocument;
            return existingDocument;
        }

        MarkdownDocument openedDocument = MarkdownDocument.Open(fullPath);
        documents.Add(openedDocument);
        ActiveDocument = openedDocument;

        return openedDocument;
    }

    public DocumentCloseResult CloseDocument(MarkdownDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        int documentIndex = documents.IndexOf(document);
        if (documentIndex < 0)
        {
            throw new ArgumentException("Document is not open in this workspace.", nameof(document));
        }

        if (document.IsDirty)
        {
            return DocumentCloseResult.UnsavedChangesNeedUserChoice;
        }

        bool closingActiveDocument = ReferenceEquals(ActiveDocument, document);
        documents.RemoveAt(documentIndex);

        if (closingActiveDocument)
        {
            ActiveDocument = GetNextActiveDocument(documentIndex);
        }

        return DocumentCloseResult.Closed;
    }

    private MarkdownDocument? GetNextActiveDocument(int closedDocumentIndex)
    {
        if (documents.Count == 0)
        {
            return null;
        }

        int nextDocumentIndex = Math.Min(closedDocumentIndex, documents.Count - 1);
        return documents[nextDocumentIndex];
    }
}
