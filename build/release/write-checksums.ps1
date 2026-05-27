param(
    [Parameter(Mandatory)]
    [string]$ArtifactDir
)

$ErrorActionPreference = "Stop"

$resolvedArtifactDir = Resolve-Path $ArtifactDir
$checksumPath = Join-Path $resolvedArtifactDir "SHA256SUMS.txt"

Get-ChildItem $resolvedArtifactDir -File |
    Where-Object { $_.Name -ne "SHA256SUMS.txt" } |
    Sort-Object Name |
    ForEach-Object {
        $hash = Get-FileHash -Algorithm SHA256 -Path $_.FullName
        "$($hash.Hash.ToLowerInvariant())  $($_.Name)"
    } |
    Set-Content -Path $checksumPath

Write-Host "Wrote $checksumPath"
