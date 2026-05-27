using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DragonMarkdown.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
