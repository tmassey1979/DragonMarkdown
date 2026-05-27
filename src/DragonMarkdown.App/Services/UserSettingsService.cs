using System.Text.Json;

namespace DragonMarkdown.App.Services;

public interface IUserSettingsService
{
    UserSettings Load();

    void Save(UserSettings settings);
}

public sealed record UserSettings(string? LastWorkspacePath, string Theme, bool WordWrap)
{
    public static UserSettings Default { get; } = new(null, "System", true);
}

public sealed class UserSettingsService : IUserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string settingsPath;

    public UserSettingsService(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
        }

        this.settingsPath = settingsPath;
    }

    public UserSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return UserSettings.Default;
        }

        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? UserSettings.Default;
        }
        catch (JsonException)
        {
            return UserSettings.Default;
        }
        catch (IOException)
        {
            return UserSettings.Default;
        }
    }

    public void Save(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
