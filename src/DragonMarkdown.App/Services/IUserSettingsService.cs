namespace DragonMarkdown.App.Services;

public interface IUserSettingsService
{
    UserSettings Load();

    void Save(UserSettings settings);
}
