param(
    [string]$Version = "0.1.0",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("win-x64", "linux-x64", "osx-x64", "osx-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "src/DragonMarkdown.App/DragonMarkdown.App.csproj"
$publishDir = Join-Path $repoRoot "artifacts/publish/$Runtime"
$installerDir = Join-Path $repoRoot "artifacts/installers"

New-Item -ItemType Directory -Force -Path $publishDir, $installerDir | Out-Null

dotnet publish $appProject `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:Version=$Version

if ($SkipInstaller) {
    Write-Host "Published DragonMarkdown $Version for $Runtime to $publishDir"
    return
}

switch -Wildcard ($Runtime) {
    "win-*" {
        & (Join-Path $PSScriptRoot "windows/package-windows.ps1") `
            -Version $Version `
            -Configuration $Configuration `
            -Runtime $Runtime `
            -PublishDir $publishDir `
            -InstallerDir $installerDir
    }
    "linux-*" {
        & (Join-Path $PSScriptRoot "linux/package-linux.ps1") `
            -Version $Version `
            -Configuration $Configuration `
            -Runtime $Runtime `
            -PublishDir $publishDir `
            -InstallerDir $installerDir
    }
    "osx-*" {
        & (Join-Path $PSScriptRoot "macos/package-macos.ps1") `
            -Version $Version `
            -Configuration $Configuration `
            -Runtime $Runtime `
            -PublishDir $publishDir `
            -InstallerDir $installerDir
    }
}

& (Join-Path $PSScriptRoot "release/write-checksums.ps1") -ArtifactDir $installerDir
