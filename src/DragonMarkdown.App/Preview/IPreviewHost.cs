using Avalonia.Controls;

namespace DragonMarkdown.App.Preview;

public interface IPreviewHost : IDisposable
{
    Control View { get; }

    void ShowHtml(string html);

    void ScrollToAnchor(string slug);
}
