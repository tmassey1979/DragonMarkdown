param(
    [string]$ArtifactDir = "artifacts/installers"
)

$ErrorActionPreference = "Stop"

$requiredArtifacts = @(
    "DragonMarkdown-0.1.0.3-win-x64.msi",
    "DragonMarkdown-0.1.0.3-osx-x64.dmg",
    "dragonmarkdown_0.1.0.3_amd64.deb",
    "dragonmarkdown_0.1.0.3_arm64.deb",
    "dragonmarkdown-0.1.0.3.x86_64.rpm",
    "dragonmarkdown-0.1.0.3.aarch64.rpm",
    "SHA256SUMS.txt"
)

$resolvedArtifactDir = Resolve-Path $ArtifactDir
$missingArtifacts = @(
    foreach ($artifact in $requiredArtifacts) {
        if (-not (Test-Path -LiteralPath (Join-Path $resolvedArtifactDir $artifact))) {
            $artifact
        }
    }
)

if ($missingArtifacts.Count -gt 0) {
    $message = "Missing v0.1.0.3 release artifacts: " + ($missingArtifacts -join ", ")
    throw $message
}

Write-Host "Validated DragonMarkdown v0.1.0.3 artifacts in $resolvedArtifactDir"
