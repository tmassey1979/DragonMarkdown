using DragonMarkdown.App.Services;

namespace DragonMarkdown.App.Tests.Services;

public sealed class RecentItemsServiceTests : IDisposable
{
    private readonly string temporaryDirectory;
    private readonly string settingsPath;

    public RecentItemsServiceTests()
    {
        temporaryDirectory = Path.Combine(Path.GetTempPath(), "DragonMarkdown.App.Services.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        settingsPath = Path.Combine(temporaryDirectory, "recent.json");
    }

    [Fact]
    public void AddRecentItem_persists_items_under_injected_settings_path()
    {
        var service = new RecentItemsService(settingsPath);

        service.AddRecentItem(@"C:\workspace\README.md");
        service.AddRecentItem(@"C:\workspace\docs");

        var reloaded = new RecentItemsService(settingsPath);
        IReadOnlyList<RecentItem> items = reloaded.GetRecentItems();

        Assert.Equal(2, items.Count);
        Assert.Equal(@"C:\workspace\docs", items[0].Path);
        Assert.Equal(@"C:\workspace\README.md", items[1].Path);
    }

    [Fact]
    public void GetRecentItems_returns_empty_list_for_missing_or_corrupt_json()
    {
        var missingService = new RecentItemsService(settingsPath);
        Assert.Empty(missingService.GetRecentItems());

        File.WriteAllText(settingsPath, "{not json");
        var corruptService = new RecentItemsService(settingsPath);

        Assert.Empty(corruptService.GetRecentItems());
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
