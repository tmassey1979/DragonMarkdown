param(
    [Parameter(Mandatory)]
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime = "linux-x64",
    [Parameter(Mandatory)]
    [string]$PublishDir,
    [Parameter(Mandatory)]
    [string]$InstallerDir
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$nfpmConfig = Join-Path $repoRoot "packaging/linux/nfpm.yaml"
$nfpm = Get-Command nfpm -ErrorAction Stop

$env:DRAGONMARKDOWN_VERSION = $Version
$env:DRAGONMARKDOWN_RUNTIME = $Runtime
$env:DRAGONMARKDOWN_PUBLISH_DIR = (Resolve-Path $PublishDir).Path
$env:DRAGONMARKDOWN_PACKAGE_DIR = (Resolve-Path $InstallerDir).Path

& $nfpm.Source package --config $nfpmConfig --packager deb --target (Join-Path $InstallerDir "dragonmarkdown_$Version_amd64.deb")
& $nfpm.Source package --config $nfpmConfig --packager rpm --target (Join-Path $InstallerDir "dragonmarkdown-$Version.x86_64.rpm")

Write-Host "Created Linux packages in $InstallerDir"
