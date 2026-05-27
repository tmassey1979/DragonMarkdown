# Installer Strategy

DragonMarkdown should ship professional native installers from GitHub Actions.

## Release Targets

| Platform | Artifact | Runner |
| --- | --- | --- |
| Windows | MSI | `windows-latest` or signed self-hosted Windows runner |
| macOS | DMG containing `.app` bundle | `macos-latest` or signed self-hosted macOS runner |
| Ubuntu | DEB | `ubuntu-latest` |
| Linux Mint | DEB | `ubuntu-latest` |
| RedHat family | RPM | `ubuntu-latest` with package tooling or native RedHat runner |

## Packaging Principles

- Publish self-contained .NET outputs.
- Package native installers from platform-specific publish folders.
- Include app icon, desktop/start menu entry, uninstall support, and version metadata.
- Keep signing and notarization secrets in GitHub Actions secrets.
- Generate checksums for every release artifact.

## Windows MSI

Recommended tool: WiX Toolset.

Installer requirements:

- Per-machine or per-user install mode decision.
- Start Menu shortcut.
- Program Files install path.
- Clean uninstall.
- Upgrade code for in-place upgrades.
- Optional `.md`, `.markdown`, and `.mdown` file association.
- Authenticode signing.

## macOS DMG

Recommended tools: `codesign`, `notarytool`, `hdiutil`.

Installer requirements:

- Proper `.app` bundle with `Info.plist`.
- `.icns` app icon.
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

## Next Implementation Slice

1. Add app icon assets.
2. Add version metadata to the app project.
3. Add `packaging/windows`, `packaging/macos`, and `packaging/linux`.
4. Add package scripts that can run locally on native hosts.
5. Extend `.github/workflows/release.yml` to produce MSI, DMG, DEB, and RPM.
