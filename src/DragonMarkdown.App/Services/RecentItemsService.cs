using System.Text.Json;

namespace DragonMarkdown.App.Services;

public interface IRecentItemsService
{
    IReadOnlyList<RecentItem> GetRecentItems();

    void AddRecentItem(string path);

    void Clear();
}

public sealed record RecentItem(string Path, DateTimeOffset LastOpenedAt);

public sealed class RecentItemsService : IRecentItemsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string settingsPath;

    public RecentItemsService(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
        }

        this.settingsPath = settingsPath;
    }

    public IReadOnlyList<RecentItem> GetRecentItems() => Load();

    public void AddRecentItem(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var items = Load()
            .Where(item => !string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
            .Prepend(new RecentItem(path, DateTimeOffset.UtcNow))
            .Take(20)
            .ToArray();

        Save(items);
    }

    public void Clear() => Save([]);

    private IReadOnlyList<RecentItem> Load()
    {
        if (!File.Exists(settingsPath))
        {
            return [];
        }

        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<RecentItem[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void Save(IReadOnlyList<RecentItem> items)
    {
        string? directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(items, JsonOptions));
    }
}
