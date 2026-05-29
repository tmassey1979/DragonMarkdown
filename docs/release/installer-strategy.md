# Installer Strategy

DragonMarkdown should ship professional native installers from GitHub Actions.

## Release Targets

| Platform | Artifact | Runner |
| --- | --- | --- |
| Windows x64 | `DragonMarkdown-<version>-win-x64.msi` | `windows-latest` or signed self-hosted Windows runner |
| macOS x64 | `DragonMarkdown-<version>-osx-x64.dmg` | `macos-latest` or signed self-hosted macOS runner |
| Linux x64 DEB | `dragonmarkdown_<version>_amd64.deb` | `ubuntu-latest` |
| Linux x64 RPM | `dragonmarkdown-<version>.x86_64.rpm` | `ubuntu-latest` |
| Raspberry Pi / Linux arm64 DEB | `dragonmarkdown_<version>_arm64.deb` | `ubuntu-latest` |
| Raspberry Pi / Linux arm64 RPM | `dragonmarkdown-<version>.aarch64.rpm` | `ubuntu-latest` |

## Packaging Principles

- Publish self-contained .NET outputs.
- Package native installers from platform-specific publish folders.
- Include app icon, desktop/start menu entry, uninstall support, and version metadata.
- Keep signing and notarization secrets in GitHub Actions secrets.
- Generate checksums for every release artifact.
- Generate release checksums once in the publish job after all installer artifacts are downloaded, so multiple build jobs cannot upload colliding `SHA256SUMS.txt` files.

## Windows MSI

Recommended tool: WiX Toolset.

Installer requirements:

- Per-machine or per-user install mode decision.
- Start Menu shortcut.
- Program Files install path.
- Clean uninstall.
- Upgrade code for in-place upgrades.
- MSI `ProductVersion` is derived from the first three parts of the public version. For v0.1.0.3, the MSI product version is `0.1.0`, while filenames, tags, and app metadata remain `0.1.0.3`.
- Optional `.md`, `.markdown`, and `.mdown` file association.
- Authenticode signing.

## macOS DMG

Recommended tools: `codesign`, `notarytool`, `hdiutil`.

Installer requirements:

- Proper `.app` bundle with `Info.plist`.
- `dragonmarkdown.icns` app icon copied into `Contents/Resources`.
- Developer ID signing.
- Notarization.
- Stapled ticket.
- DMG background and Applications symlink when design assets are ready.

## Linux DEB/RPM

Recommended tool: `nfpm`.

Installer requirements:

- Install under `/opt/dragonmarkdown`.
- Add `/usr/bin/dragonmarkdown` launcher.
- Add `.desktop` entry.
- Add app icon.
- Register markdown MIME types.
- Clean uninstall.

## Repository Support

- `build/package.ps1` is the cross-platform package entry point for `win-x64`, `linux-x64`, `linux-arm64`, and `osx-x64`.
- `packaging/windows/DragonMarkdown.wxs` defines the MSI.
- `packaging/macos/Info.plist` defines the `.app` bundle metadata.
- `packaging/linux/nfpm.yaml` defines DEB/RPM metadata.
- `.github/workflows/release.yml` runs native package jobs and publishes release artifacts.

## Next Implementation Slice

1. Add signed installer validation on self-hosted or secret-backed runners.
2. Add file association tests for installed packages.
3. Add release smoke tests on clean OS images.
