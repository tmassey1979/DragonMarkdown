namespace DragonMarkdown.App.Services;

public static class AppDataPaths
{
    public static string SettingsPath => Path.Combine(AppDataRoot, "settings.json");

    public static string RecentItemsPath => Path.Combine(AppDataRoot, "recent-items.json");

    public static string AutosaveRecoveryRoot => Path.Combine(AppDataRoot, "autosave");

    private static string AppDataRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DragonMarkdown");
}
