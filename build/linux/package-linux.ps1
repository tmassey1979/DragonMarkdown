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
$generatedConfigDir = Join-Path $repoRoot "artifacts/package-config/$Runtime"
$generatedConfig = Join-Path $generatedConfigDir "nfpm.yaml"
$nfpm = Get-Command nfpm -ErrorAction Stop

$publishPath = (Resolve-Path $PublishDir).Path.Replace("\", "/")

New-Item -ItemType Directory -Force -Path $generatedConfigDir | Out-Null

$config = Get-Content $nfpmConfig -Raw
$config = $config.Replace('${DRAGONMARKDOWN_VERSION}', $Version)
$config = $config.Replace('${DRAGONMARKDOWN_RUNTIME}', $Runtime)
$config = $config.Replace('${DRAGONMARKDOWN_PUBLISH_DIR}', $publishPath)
Set-Content -Path $generatedConfig -Value $config -NoNewline

& $nfpm.Source package --config $generatedConfig --packager deb --target (Join-Path $InstallerDir "dragonmarkdown_$Version_amd64.deb")
& $nfpm.Source package --config $generatedConfig --packager rpm --target (Join-Path $InstallerDir "dragonmarkdown-$Version.x86_64.rpm")

Write-Host "Created Linux packages in $InstallerDir"
