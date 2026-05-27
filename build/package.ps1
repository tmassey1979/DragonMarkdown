param(
    [string]$Version = "0.1.0.2",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("win-x64", "linux-x64", "linux-arm64", "osx-x64")]
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "src/DragonMarkdown.App/DragonMarkdown.App.csproj"
$appProjectDir = Split-Path $appProject -Parent
$publishDir = Join-Path $repoRoot "artifacts/publish/$Runtime"
$installerDir = Join-Path $repoRoot "artifacts/installers"

New-Item -ItemType Directory -Force -Path $publishDir, $installerDir | Out-Null

if ($Runtime.StartsWith("osx-", [StringComparison]::Ordinal)) {
    dotnet build $appProject `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        -p:Version=$Version
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $Runtime."
    }

    $runtimeBuildDir = Join-Path $appProjectDir "bin/$Configuration/net10.0/$Runtime"
    foreach ($missingCefAsset in @("libEGL.dylib", "libGLESv2.dylib", "libvk_swiftshader.dylib")) {
        $assetPath = Join-Path $runtimeBuildDir $missingCefAsset
        if (-not (Test-Path $assetPath)) {
            New-Item -ItemType File -Force -Path $assetPath | Out-Null
        }
    }

    dotnet publish $appProject `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        --no-build `
        --output $publishDir `
        -p:Version=$Version
} else {
    dotnet publish $appProject `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        --output $publishDir `
        -p:Version=$Version
}

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for $Runtime."
}

if ($Runtime.StartsWith("osx-", [StringComparison]::Ordinal)) {
    foreach ($missingCefAsset in @("libEGL.dylib", "libGLESv2.dylib", "libvk_swiftshader.dylib")) {
        Remove-Item (Join-Path $publishDir $missingCefAsset) -Force -ErrorAction SilentlyContinue
    }
}

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

