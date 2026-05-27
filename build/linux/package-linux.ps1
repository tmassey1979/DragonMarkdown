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

$runtimePackage = switch ($Runtime) {
    "linux-x64" {
        @{
            NfpmArch = "amd64"
            DebArch = "amd64"
            RpmArch = "x86_64"
        }
    }
    "linux-arm64" {
        @{
            NfpmArch = "arm64"
            DebArch = "arm64"
            RpmArch = "aarch64"
        }
    }
    default {
        throw "Unsupported Linux runtime '$Runtime'. Supported runtimes: linux-x64, linux-arm64."
    }
}

$publishPath = (Resolve-Path $PublishDir).Path.Replace("\", "/")
$NfpmArch = $runtimePackage.NfpmArch
$DebArch = $runtimePackage.DebArch
$RpmArch = $runtimePackage.RpmArch

New-Item -ItemType Directory -Force -Path $generatedConfigDir | Out-Null

$config = Get-Content $nfpmConfig -Raw
$config = $config.Replace('${DRAGONMARKDOWN_VERSION}', $Version)
$config = $config.Replace('${DRAGONMARKDOWN_RUNTIME}', $Runtime)
$config = $config.Replace('${DRAGONMARKDOWN_ARCH}', $NfpmArch)
$config = $config.Replace('${DRAGONMARKDOWN_PUBLISH_DIR}', $publishPath)
Set-Content -Path $generatedConfig -Value $config -NoNewline

& $nfpm.Source package --config $generatedConfig --packager deb --target (Join-Path $InstallerDir "dragonmarkdown_${Version}_${DebArch}.deb")
if ($LASTEXITCODE -ne 0) {
    throw "nFPM DEB packaging failed for $Runtime."
}

& $nfpm.Source package --config $generatedConfig --packager rpm --target (Join-Path $InstallerDir "dragonmarkdown-$Version.$RpmArch.rpm")
if ($LASTEXITCODE -ne 0) {
    throw "nFPM RPM packaging failed for $Runtime."
}

Write-Host "Created Linux packages in $InstallerDir"
