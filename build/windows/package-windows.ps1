param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [Parameter(Mandatory)]
    [string]$PublishDir,
    [Parameter(Mandatory)]
    [string]$InstallerDir
)

$ErrorActionPreference = "Stop"

function ConvertTo-MsiProductVersion {
    param(
        [Parameter(Mandatory)]
        [string]$Version
    )

    $parts = $Version.Split(".")
    if ($parts.Length -lt 3) {
        throw "MSI ProductVersion requires at least three version parts. Received '$Version'."
    }

    return ($parts[0..2] -join ".")
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$wxsPath = Join-Path $repoRoot "packaging/windows/DragonMarkdown.wxs"
$iconPath = Join-Path $repoRoot "packaging/assets/dragonmarkdown.ico"
$resolvedPublishDir = (Resolve-Path $PublishDir).Path
$resolvedInstallerDir = (Resolve-Path $InstallerDir).Path
$msiPath = Join-Path $resolvedInstallerDir "DragonMarkdown-$Version-$Runtime.msi"
$productVersion = ConvertTo-MsiProductVersion -Version $Version

dotnet tool restore

dotnet tool run wix build $wxsPath `
    -define "PublishDir=$resolvedPublishDir" `
    -define "ProductVersion=$productVersion" `
    -define "IconPath=$iconPath" `
    -out $msiPath

if ($env:WINDOWS_CODESIGN_CERT_BASE64 -and $env:WINDOWS_CODESIGN_CERT_PASSWORD) {
    $certPath = Join-Path $env:RUNNER_TEMP "dragonmarkdown-codesign.pfx"
    [IO.File]::WriteAllBytes($certPath, [Convert]::FromBase64String($env:WINDOWS_CODESIGN_CERT_BASE64))

    $signtool = Get-Command signtool.exe -ErrorAction Stop
    & $signtool.Source sign `
        /f $certPath `
        /p $env:WINDOWS_CODESIGN_CERT_PASSWORD `
        /fd SHA256 `
        /tr "http://timestamp.digicert.com" `
        /td SHA256 `
        $msiPath
}

Write-Host "Created $msiPath"
