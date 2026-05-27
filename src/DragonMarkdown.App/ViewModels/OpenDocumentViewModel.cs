using CommunityToolkit.Mvvm.ComponentModel;
using DragonMarkdown.Core.Documents;

namespace DragonMarkdown.App.ViewModels;

public sealed partial class OpenDocumentViewModel : ObservableObject
{
    public OpenDocumentViewModel(MarkdownDocument document)
    {
        Document = document;
        text = document.Text;
    }

    public MarkdownDocument Document { get; }

    public string DisplayName => Document.DisplayName;

    public bool IsDirty => Document.IsDirty;

    [ObservableProperty]
    private string text;

    public event EventHandler? TextChanged;

    partial void OnTextChanged(string value)
    {
        Document.UpdateText(value);
        OnPropertyChanged(nameof(IsDirty));
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        Document.Save();
        OnPropertyChanged(nameof(IsDirty));
    }
}
