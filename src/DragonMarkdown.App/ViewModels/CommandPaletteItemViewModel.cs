using System.Windows.Input;

namespace DragonMarkdown.App.ViewModels;

public sealed class CommandPaletteItemViewModel
{
    public CommandPaletteItemViewModel(
        string title,
        string category,
        string? shortcutText,
        string keywords,
        ICommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(command);

        Title = title;
        Category = category;
        ShortcutText = shortcutText;
        Keywords = keywords;
        Command = command;
    }

    public string Title { get; }

    public string Category { get; }

    public string? ShortcutText { get; }

    public string Keywords { get; }

    public ICommand Command { get; }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(Title, query)
            || Contains(Category, query)
            || Contains(Keywords, query);
    }

    public void Execute()
    {
        if (Command.CanExecute(null))
        {
            Command.Execute(null);
        }
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }
}
