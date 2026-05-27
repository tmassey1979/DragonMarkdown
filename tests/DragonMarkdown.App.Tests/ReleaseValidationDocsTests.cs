namespace DragonMarkdown.App.Tests;

public sealed class ReleaseValidationDocsTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ReleaseChecklistNamesEveryV0102InstallerArtifact()
    {
        var checklist = Read("docs/release/release-checklist.md");

        Assert.Contains("DragonMarkdown-0.1.0.2-win-x64.msi", checklist, StringComparison.Ordinal);
        Assert.Contains("DragonMarkdown-0.1.0.2-osx-x64.dmg", checklist, StringComparison.Ordinal);
        Assert.Contains("DragonMarkdown-0.1.0.2-osx-arm64.dmg", checklist, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown_0.1.0.2_amd64.deb", checklist, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown_0.1.0.2_arm64.deb", checklist, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown-0.1.0.2.x86_64.rpm", checklist, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown-0.1.0.2.aarch64.rpm", checklist, StringComparison.Ordinal);
        Assert.Contains("SHA256SUMS.txt", checklist, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseDocsDefinePackageLevelCoverageGateAndRaspberryPiValidation()
    {
        var checklist = Read("docs/release/release-checklist.md");
        var buildAndTest = Read("docs/wiki/Build-And-Test.md");
        var cleanCode = Read("docs/wiki/Clean-Code-Standards.md");
        var githubActions = Read("docs/ci/github-actions.md");

        foreach (var document in new[] { checklist, buildAndTest, cleanCode, githubActions })
        {
            Assert.Contains("80% package-level line coverage", document, StringComparison.Ordinal);
        }

        Assert.Contains("Raspberry Pi 64-bit", checklist, StringComparison.Ordinal);
        Assert.Contains("linux-arm64", checklist, StringComparison.Ordinal);
        Assert.Contains("Raspberry Pi 64-bit", githubActions, StringComparison.Ordinal);
        Assert.Contains("linux-arm64", githubActions, StringComparison.Ordinal);
    }

    [Fact]
    public void ArtifactValidationScriptTracksAllV0102PackageNames()
    {
        var script = Read("build/release/validate-v0.1.0.2-artifacts.ps1");

        Assert.Contains("DragonMarkdown-0.1.0.2-win-x64.msi", script, StringComparison.Ordinal);
        Assert.Contains("DragonMarkdown-0.1.0.2-osx-x64.dmg", script, StringComparison.Ordinal);
        Assert.Contains("DragonMarkdown-0.1.0.2-osx-arm64.dmg", script, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown_0.1.0.2_amd64.deb", script, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown_0.1.0.2_arm64.deb", script, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown-0.1.0.2.x86_64.rpm", script, StringComparison.Ordinal);
        Assert.Contains("dragonmarkdown-0.1.0.2.aarch64.rpm", script, StringComparison.Ordinal);
        Assert.Contains("SHA256SUMS.txt", script, StringComparison.Ordinal);
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DragonMarkdown.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find DragonMarkdown repository root.");
    }
}
