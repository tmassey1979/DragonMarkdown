namespace DragonMarkdown.App.Services;

public interface IUpdateCheckService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken = default);
}
