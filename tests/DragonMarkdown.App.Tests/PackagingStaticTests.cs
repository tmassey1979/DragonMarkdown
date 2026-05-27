using System.Text.RegularExpressions;

namespace DragonMarkdown.App.Tests;

public sealed class PackagingStaticTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void AppProjectWiresWindowsApplicationIcon()
    {
        string project = Read("src/DragonMarkdown.App/DragonMarkdown.App.csproj");

        Assert.Contains("<ApplicationIcon>..\\..\\packaging\\assets\\dragonmarkdown.ico</ApplicationIcon>", project);
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "packaging/assets/dragonmarkdown.ico")));
    }

    [Fact]
    public void WindowsPackagingUsesMsiSafeVersionAndShortcutIcon()
    {
        string script = Read("build/windows/package-windows.ps1");
        string wxs = Read("packaging/windows/DragonMarkdown.wxs");

        Assert.Contains("$productVersion = ConvertTo-MsiProductVersion -Version $Version", script);
        Assert.Contains("-define \"ProductVersion=$productVersion\"", script);
        Assert.Contains("-define \"IconPath=$iconPath\"", script);
        Assert.Contains("<Icon Id=\"DragonMarkdownIcon\" SourceFile=\"$(IconPath)\" />", wxs);
        Assert.Contains("Icon=\"DragonMarkdownIcon\"", wxs);
    }

    [Fact]
    public void MacPackagingDeclaresAndCopiesIcnsIcon()
    {
        string plist = Read("packaging/macos/Info.plist");
        string script = Read("build/macos/package-macos.ps1");

        Assert.Contains("<key>CFBundleIconFile</key>", plist);
        Assert.Contains("<string>dragonmarkdown.icns</string>", plist);
        Assert.Contains("packaging/assets/dragonmarkdown.icns", script);
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "packaging/assets/dragonmarkdown.icns")));
    }

    [Fact]
    public void LinuxPackagingMapsSupportedRuntimeArchitecturesAndNamesArtifacts()
    {
        string entrypoint = Read("build/package.ps1");
        string script = Read("build/linux/package-linux.ps1");
        string nfpm = Read("packaging/linux/nfpm.yaml");

        Assert.Contains("\"linux-arm64\"", entrypoint);
        Assert.Contains("\"linux-x64\" {", script);
        Assert.Contains("\"linux-arm64\" {", script);
        Assert.Contains("NfpmArch = \"amd64\"", script);
        Assert.Contains("NfpmArch = \"arm64\"", script);
        Assert.Contains("DebArch = \"amd64\"", script);
        Assert.Contains("DebArch = \"arm64\"", script);
        Assert.Contains("RpmArch = \"x86_64\"", script);
        Assert.Contains("RpmArch = \"aarch64\"", script);
        Assert.Contains("dragonmarkdown_${Version}_${DebArch}.deb", script);
        Assert.Contains("dragonmarkdown-$Version.$RpmArch.rpm", script);
        Assert.Contains("arch: ${DRAGONMARKDOWN_ARCH}", nfpm);
    }

    [Fact]
    public void ReleaseWorkflowBuildsLinuxRuntimeMatrixAndPublishesFinalChecksums()
    {
        string workflow = Read(".github/workflows/release.yml");

        Assert.Contains("name: Linux DEB and RPM (${{ matrix.rid }})", workflow);
        Assert.Contains("- linux-x64", workflow);
        Assert.Contains("- linux-arm64", workflow);
        Assert.Contains("name: linux-${{ matrix.rid }}", workflow);
        Assert.Contains("./build/package.ps1 -Version $version -Runtime ${{ matrix.rid }}", workflow);
        Assert.Contains("sha256sum * > SHA256SUMS.txt", workflow);
        Assert.DoesNotContain("SHA256SUMS.txt", ExtractUploadArtifactPathBlock(workflow));
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    }

    private static string ExtractUploadArtifactPathBlock(string workflow)
    {
        Match match = Regex.Match(
            workflow,
            "name: Upload Linux packages.*?path: (?<path>[^\\r\\n]+)",
            RegexOptions.Singleline);

        return match.Success ? match.Groups["path"].Value : string.Empty;
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
