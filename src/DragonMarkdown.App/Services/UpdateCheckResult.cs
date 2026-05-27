namespace DragonMarkdown.App.Services;

public sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string? LatestVersion,
    Uri? ReleaseUri,
    string Message);
