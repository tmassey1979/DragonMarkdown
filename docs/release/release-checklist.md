# Release Checklist

Use this checklist for every DragonMarkdown release.

## Preflight

- Confirm `main` is clean.
- Run `dotnet build .\DragonMarkdown.slnx`.
- Run `dotnet test .\DragonMarkdown.slnx --no-build`.
- Run coverage with `coverlet.runsettings`.
- Confirm each test package reports at least 80% package-level line coverage for testable code.
- Confirm app version in release tag matches the package version.
- Confirm release notes include user-facing changes and known limitations.

## Package Build

- Windows MSI created on Windows runner: `DragonMarkdown-0.1.0.2-win-x64.msi`.
- macOS DMGs created on macOS runner: `DragonMarkdown-0.1.0.2-osx-x64.dmg` and `DragonMarkdown-0.1.0.2-osx-arm64.dmg`.
- Linux DEB and RPM created on Linux runner:
  - `dragonmarkdown_0.1.0.2_amd64.deb`
  - `dragonmarkdown_0.1.0.2_arm64.deb`
  - `dragonmarkdown-0.1.0.2.x86_64.rpm`
  - `dragonmarkdown-0.1.0.2.aarch64.rpm`
- SHA256 checksums generated in `SHA256SUMS.txt`.

## Signing

- Windows MSI signed when certificate secrets are configured.
- macOS app and DMG signed when Developer ID secrets are configured.
- macOS DMG notarized and stapled when Apple credentials are configured.

## Verification

- Install on a clean Windows VM.
- Install on a clean macOS VM or test machine.
- Install DEB on Ubuntu and Mint.
- Install RPM on a RedHat-family distribution.
- Install and launch the `linux-arm64` DEB on Raspberry Pi 64-bit OS.
- Launch app from OS launcher.
- Open a folder.
- Export Word and PDF.
- Uninstall cleanly.

## Publish

- Push tag `vX.Y.Z`.
- Confirm GitHub Actions release workflow succeeds.
- Confirm GitHub Release contains all installers and `SHA256SUMS.txt`.
- Update GitHub Pages if marketing copy or download links changed.
