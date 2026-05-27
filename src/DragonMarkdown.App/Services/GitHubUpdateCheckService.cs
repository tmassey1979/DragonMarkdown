using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace DragonMarkdown.App.Services;

public sealed class GitHubUpdateCheckService : IUpdateCheckService
{
    private static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/tmassey1979/DragonMarkdown/releases/latest");
    private readonly HttpClient httpClient;

    public GitHubUpdateCheckService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DragonMarkdown", GetClientVersion()));

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            string? latestVersion = ReadString(document, "tag_name");
            string? releaseUrl = ReadString(document, "html_url");
            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                return new UpdateCheckResult(false, null, null, "Could not check for updates: latest release did not include a version.");
            }

            Uri? releaseUri = Uri.TryCreate(releaseUrl, UriKind.Absolute, out Uri? parsedUri) ? parsedUri : null;
            bool updateAvailable = IsNewerVersion(latestVersion, currentVersion);
            string message = updateAvailable
                ? $"{latestVersion} is available."
                : "DragonMarkdown is up to date.";

            return new UpdateCheckResult(updateAvailable, latestVersion, releaseUri, message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new UpdateCheckResult(false, null, null, $"Could not check for updates: {ex.Message}");
        }
    }

    private static string? ReadString(JsonDocument document, string propertyName)
    {
        return document.RootElement.TryGetProperty(propertyName, out JsonElement element)
            ? element.GetString()
            : null;
    }

    private static bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        return Version.TryParse(NormalizeVersion(latestVersion), out Version? latest)
            && Version.TryParse(NormalizeVersion(currentVersion), out Version? current)
            && latest > current;
    }

    private static string NormalizeVersion(string version)
    {
        return version.Trim().TrimStart('v', 'V');
    }

    private static string GetClientVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
    }
}
