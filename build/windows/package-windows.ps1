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

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$wxsPath = Join-Path $repoRoot "packaging/windows/DragonMarkdown.wxs"
$resolvedPublishDir = (Resolve-Path $PublishDir).Path
$resolvedInstallerDir = (Resolve-Path $InstallerDir).Path
$msiPath = Join-Path $resolvedInstallerDir "DragonMarkdown-$Version-$Runtime.msi"

dotnet tool restore

dotnet tool run wix build $wxsPath `
    -define "PublishDir=$resolvedPublishDir" `
    -define "ProductVersion=$Version" `
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
