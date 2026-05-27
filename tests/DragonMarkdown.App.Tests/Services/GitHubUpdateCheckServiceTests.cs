using System.Net;
using DragonMarkdown.App.Services;

namespace DragonMarkdown.App.Tests.Services;

public sealed class GitHubUpdateCheckServiceTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_ReturnsAvailableUpdateWhenLatestVersionIsNewer()
    {
        var service = new GitHubUpdateCheckService(CreateClient(
            HttpStatusCode.OK,
            """{"tag_name":"v9.9.9","html_url":"https://example.test/release"}"""));

        UpdateCheckResult result = await service.CheckForUpdatesAsync("0.1.0.2");

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("v9.9.9", result.LatestVersion);
        Assert.Equal(new Uri("https://example.test/release"), result.ReleaseUri);
        Assert.Equal("v9.9.9 is available.", result.Message);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ReturnsNoUpdateWhenCurrentVersionMatchesLatest()
    {
        var service = new GitHubUpdateCheckService(CreateClient(
            HttpStatusCode.OK,
            """{"tag_name":"v0.1.0.2","html_url":"https://example.test/release"}"""));

        UpdateCheckResult result = await service.CheckForUpdatesAsync("0.1.0.2");

        Assert.False(result.IsUpdateAvailable);
        Assert.Equal("DragonMarkdown is up to date.", result.Message);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ReturnsFailureMessageWhenRequestFails()
    {
        var service = new GitHubUpdateCheckService(CreateClient(HttpStatusCode.InternalServerError, "{}"));

        UpdateCheckResult result = await service.CheckForUpdatesAsync("0.1.0.2");

        Assert.False(result.IsUpdateAvailable);
        Assert.StartsWith("Could not check for updates:", result.Message, StringComparison.Ordinal);
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        return new HttpClient(new StubHttpMessageHandler(statusCode, responseBody))
        {
            BaseAddress = new Uri("https://api.github.com")
        };
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            };

            return Task.FromResult(response);
        }
    }
}
