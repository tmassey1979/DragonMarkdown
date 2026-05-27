using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DragonMarkdown.App.Services;

public interface IAutosaveRecoveryService
{
    void WriteSnapshot(string documentPath, string content);

    IReadOnlyList<AutosaveRecoverySnapshot> ListRecoverableSnapshots();

    void ClearSnapshots(string documentPath);
}

public sealed record AutosaveRecoverySnapshot(
    string DocumentPath,
    string Content,
    string SnapshotPath,
    DateTimeOffset LastSavedAt);

public sealed class AutosaveRecoveryService : IAutosaveRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string recoveryRoot;

    public AutosaveRecoveryService(string recoveryRoot)
    {
        if (string.IsNullOrWhiteSpace(recoveryRoot))
        {
            throw new ArgumentException("Recovery root is required.", nameof(recoveryRoot));
        }

        this.recoveryRoot = recoveryRoot;
    }

    public void WriteSnapshot(string documentPath, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);
        ArgumentNullException.ThrowIfNull(content);

        Directory.CreateDirectory(recoveryRoot);
        var snapshot = new AutosaveRecoverySnapshot(
            documentPath,
            content,
            GetSnapshotPath(documentPath),
            DateTimeOffset.UtcNow);

        File.WriteAllText(snapshot.SnapshotPath, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    public IReadOnlyList<AutosaveRecoverySnapshot> ListRecoverableSnapshots()
    {
        if (!Directory.Exists(recoveryRoot))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(recoveryRoot, "*.json")
            .Select(ReadSnapshot)
            .Where(snapshot => snapshot is not null)
            .Cast<AutosaveRecoverySnapshot>()
            .OrderByDescending(snapshot => snapshot.LastSavedAt)
            .ToArray();
    }

    public void ClearSnapshots(string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        string snapshotPath = GetSnapshotPath(documentPath);
        if (File.Exists(snapshotPath))
        {
            File.Delete(snapshotPath);
        }
    }

    private AutosaveRecoverySnapshot? ReadSnapshot(string snapshotPath)
    {
        try
        {
            string json = File.ReadAllText(snapshotPath);
            AutosaveRecoverySnapshot? snapshot = JsonSerializer.Deserialize<AutosaveRecoverySnapshot>(json, JsonOptions);
            return snapshot is null ? null : snapshot with { SnapshotPath = snapshotPath };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private string GetSnapshotPath(string documentPath)
    {
        byte[] pathBytes = Encoding.UTF8.GetBytes(Path.GetFullPath(documentPath));
        byte[] hashBytes = SHA256.HashData(pathBytes);
        string fileName = Convert.ToHexString(hashBytes).ToLowerInvariant() + ".json";
        return Path.Combine(recoveryRoot, fileName);
    }
}
