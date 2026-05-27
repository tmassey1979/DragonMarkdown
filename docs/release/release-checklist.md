# Release Checklist

Use this checklist for every DragonMarkdown release.

## Preflight

- Confirm `main` is clean.
- Run `dotnet build .\DragonMarkdown.slnx`.
- Run `dotnet test .\DragonMarkdown.slnx --no-build`.
- Run coverage with `coverlet.runsettings`.
- Confirm app version in release tag matches the package version.
- Confirm release notes include user-facing changes and known limitations.

## Package Build

- Windows MSI created on Windows runner.
- macOS DMGs created on macOS runner for `osx-x64` and `osx-arm64`.
- Linux DEB and RPM created on Linux runner.
- SHA256 checksums generated.

## Signing

- Windows MSI signed when certificate secrets are configured.
- macOS app and DMG signed when Developer ID secrets are configured.
- macOS DMG notarized and stapled when Apple credentials are configured.

## Verification

- Install on a clean Windows VM.
- Install on a clean macOS VM or test machine.
- Install DEB on Ubuntu and Mint.
- Install RPM on a RedHat-family distribution.
- Launch app from OS launcher.
- Open a folder.
- Export Word and PDF.
- Uninstall cleanly.

## Publish

- Push tag `vX.Y.Z`.
- Confirm GitHub Actions release workflow succeeds.
- Confirm GitHub Release contains all installers and `SHA256SUMS.txt`.
- Update GitHub Pages if marketing copy or download links changed.
