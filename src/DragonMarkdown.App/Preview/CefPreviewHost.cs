using Avalonia.Controls;
using WebViewControl;

namespace DragonMarkdown.App.Preview;

public sealed class CefPreviewHost : IPreviewHost
{
    private readonly WebView browser = new();

    public Control View => browser;

    public void ShowHtml(string html)
    {
        var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(html));
        browser.Address = $"data:text/html;base64,{encoded}";
    }

    public void Dispose()
    {
        browser.Dispose();
    }
}
