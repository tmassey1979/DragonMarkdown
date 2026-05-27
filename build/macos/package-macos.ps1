param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Configuration = "Release",
    [Parameter(Mandatory)]
    [string]$Runtime,
    [Parameter(Mandatory)]
    [string]$PublishDir,
    [Parameter(Mandatory)]
    [string]$InstallerDir
)

$ErrorActionPreference = "Stop"

if (-not $IsMacOS) {
    throw "macOS packaging must run on a macOS runner."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$bundleRoot = Join-Path $repoRoot "artifacts/bundles/$Runtime/DragonMarkdown.app"
$contentsDir = Join-Path $bundleRoot "Contents"
$macOsDir = Join-Path $contentsDir "MacOS"
$resourcesDir = Join-Path $contentsDir "Resources"
$dmgPath = Join-Path $InstallerDir "DragonMarkdown-$Version-$Runtime.dmg"

Remove-Item $bundleRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $macOsDir, $resourcesDir | Out-Null

Copy-Item (Join-Path $PublishDir "*") $macOsDir -Recurse -Force
Copy-Item (Join-Path $repoRoot "packaging/assets/dragonmarkdown.svg") (Join-Path $resourcesDir "dragonmarkdown.svg") -Force
Copy-Item (Join-Path $repoRoot "packaging/assets/dragonmarkdown.icns") (Join-Path $resourcesDir "dragonmarkdown.icns") -Force

$infoPlist = Get-Content (Join-Path $repoRoot "packaging/macos/Info.plist") -Raw
$infoPlist = $infoPlist.Replace("{{VERSION}}", $Version)
$infoPlist = $infoPlist.Replace("{{RUNTIME}}", $Runtime)
Set-Content -Path (Join-Path $contentsDir "Info.plist") -Value $infoPlist -NoNewline

chmod +x (Join-Path $macOsDir "DragonMarkdown.App")

if ($env:APPLE_DEVELOPER_ID) {
    codesign --force --deep --options runtime --sign $env:APPLE_DEVELOPER_ID $bundleRoot
    if ($LASTEXITCODE -ne 0) {
        throw "macOS code signing failed for $bundleRoot."
    }
}

Remove-Item $dmgPath -Force -ErrorAction SilentlyContinue
hdiutil create -volname "DragonMarkdown" -srcfolder $bundleRoot -ov -format UDZO $dmgPath
if ($LASTEXITCODE -ne 0) {
    throw "macOS DMG creation failed for $Runtime."
}

if ($env:APPLE_DEVELOPER_ID -and $env:APPLE_APP_SPECIFIC_PASSWORD -and $env:APPLE_TEAM_ID) {
    xcrun notarytool submit $dmgPath `
        --apple-id $env:APPLE_DEVELOPER_ID `
        --password $env:APPLE_APP_SPECIFIC_PASSWORD `
        --team-id $env:APPLE_TEAM_ID `
        --wait
    if ($LASTEXITCODE -ne 0) {
        throw "macOS notarization failed for $dmgPath."
    }

    xcrun stapler staple $dmgPath
    if ($LASTEXITCODE -ne 0) {
        throw "macOS stapling failed for $dmgPath."
    }
}

Write-Host "Created $dmgPath"
