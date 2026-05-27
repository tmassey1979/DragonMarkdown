# Packaging And Releases

DragonMarkdown should ship with native installers for each supported desktop platform.

## Windows

Target:

- Self-contained `win-x64` publish
- MSI installer
- Start Menu shortcut
- Uninstall support
- Optional markdown file association
- Code signing

Recommended tooling:

- WiX Toolset

## macOS

Target:

- `.app` bundle
- DMG installer
- `osx-x64` and `osx-arm64`
- Developer ID signing
- Notarization

Recommended tooling:

- Native macOS runner
- Xcode command line tools
- `codesign`, `notarytool`, `hdiutil`

## Ubuntu And Mint

Target:

- DEB package
- Desktop entry
- Icon
- MIME association for markdown files

Recommended tooling:

- `nfpm`

## RedHat Family

Target:

- RPM package
- Desktop entry
- Icon
- MIME association for markdown files

Recommended tooling:

- `nfpm`

## Release Flow

1. Tag the release.
2. Native runners build platform artifacts.
3. CI uploads installers and checksums.
4. Release notes list installer hashes and supported platforms.
