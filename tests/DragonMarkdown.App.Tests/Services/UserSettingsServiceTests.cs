using DragonMarkdown.App.Services;

namespace DragonMarkdown.App.Tests.Services;

public sealed class UserSettingsServiceTests : IDisposable
{
    private readonly string temporaryDirectory;
    private readonly string settingsPath;

    public UserSettingsServiceTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.App.Settings.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        settingsPath = Path.Combine(temporaryDirectory, "settings.json");
    }

    [Fact]
    public void Save_persists_settings_under_injected_settings_path()
    {
        var service = new UserSettingsService(settingsPath);
        var settings = new UserSettings(
            LastWorkspacePath: @"C:\workspace",
            Theme: "Dark",
            WordWrap: false);

        service.Save(settings);

        var reloaded = new UserSettingsService(settingsPath);
        UserSettings loaded = reloaded.Load();

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public void Load_returns_defaults_for_missing_or_corrupt_json()
    {
        var missingService = new UserSettingsService(settingsPath);
        Assert.Equal(UserSettings.Default, missingService.Load());

        File.WriteAllText(settingsPath, "{not json");
        var corruptService = new UserSettingsService(settingsPath);

        Assert.Equal(UserSettings.Default, corruptService.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
