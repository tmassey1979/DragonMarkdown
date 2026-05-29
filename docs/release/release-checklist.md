# Release Checklist

Use this checklist for every DragonMarkdown release.

## Preflight

- Confirm `main` is clean.
- Run `dotnet build .\DragonMarkdown.slnx`.
- Run `dotnet test .\DragonMarkdown.slnx --no-build`.
- Run coverage with `coverlet.runsettings`.
- Confirm each test package reports at least 80% package-level line coverage for testable code.
- Confirm app version in release tag matches the package version.
- For v0.1.0.3, confirm Windows MSI metadata uses `ProductVersion=0.1.0` while the release tag, app metadata, and installer filenames use `0.1.0.3`.
- Confirm release notes include user-facing changes and known limitations.

## Package Build

- Windows MSI created on Windows runner: `DragonMarkdown-0.1.0.3-win-x64.msi`.
- macOS DMG created on macOS runner: `DragonMarkdown-0.1.0.3-osx-x64.dmg`.
- Linux DEB and RPM created on Linux runner:
  - `dragonmarkdown_0.1.0.3_amd64.deb`
  - `dragonmarkdown_0.1.0.3_arm64.deb`
  - `dragonmarkdown-0.1.0.3.x86_64.rpm`
  - `dragonmarkdown-0.1.0.3.aarch64.rpm`
- SHA256 checksums generated once in the publish job after artifact download.

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
- Confirm GitHub Release contains the Windows MSI, macOS DMGs, Linux x64 DEB/RPM, Linux arm64 DEB/RPM, and `SHA256SUMS.txt`.
- Update GitHub Pages if marketing copy or download links changed.
