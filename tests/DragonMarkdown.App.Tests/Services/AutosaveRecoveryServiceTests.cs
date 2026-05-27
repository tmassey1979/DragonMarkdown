using DragonMarkdown.App.Services;

namespace DragonMarkdown.App.Tests.Services;

public sealed class AutosaveRecoveryServiceTests : IDisposable
{
    private readonly string recoveryRoot;

    public AutosaveRecoveryServiceTests()
    {
        recoveryRoot = Path.Combine(Path.GetTempPath(), "DragonMarkdown.App.Recovery.Tests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void WriteSnapshot_persists_recoverable_snapshot_under_injected_recovery_root()
    {
        var service = new AutosaveRecoveryService(recoveryRoot);

        service.WriteSnapshot(@"C:\workspace\README.md", "# Draft");

        IReadOnlyList<AutosaveRecoverySnapshot> snapshots = service.ListRecoverableSnapshots();
        Assert.Single(snapshots);
        Assert.Equal(@"C:\workspace\README.md", snapshots[0].DocumentPath);
        Assert.Equal("# Draft", snapshots[0].Content);
        Assert.True(File.Exists(snapshots[0].SnapshotPath));
    }

    [Fact]
    public void ListRecoverableSnapshots_skips_corrupt_json()
    {
        Directory.CreateDirectory(recoveryRoot);
        File.WriteAllText(Path.Combine(recoveryRoot, "bad.json"), "{not json");
        var service = new AutosaveRecoveryService(recoveryRoot);

        IReadOnlyList<AutosaveRecoverySnapshot> snapshots = service.ListRecoverableSnapshots();

        Assert.Empty(snapshots);
    }

    [Fact]
    public void ClearSnapshots_removes_snapshots_after_save()
    {
        var service = new AutosaveRecoveryService(recoveryRoot);
        service.WriteSnapshot(@"C:\workspace\README.md", "# Draft");
        service.WriteSnapshot(@"C:\workspace\notes.md", "# Notes");

        service.ClearSnapshots(@"C:\workspace\README.md");

        IReadOnlyList<AutosaveRecoverySnapshot> snapshots = service.ListRecoverableSnapshots();
        Assert.Single(snapshots);
        Assert.Equal(@"C:\workspace\notes.md", snapshots[0].DocumentPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(recoveryRoot))
        {
            Directory.Delete(recoveryRoot, recursive: true);
        }
    }
}
