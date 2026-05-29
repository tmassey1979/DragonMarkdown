# Packaging And Releases

DragonMarkdown should ship with native installers for each supported desktop platform.

## Windows

Target:

- Self-contained `win-x64` publish
- MSI installer named `DragonMarkdown-<version>-win-x64.msi`
- Start Menu shortcut
- App icon in the shortcut and Add/Remove Programs
- Uninstall support
- Public four-part app versions are supported. For v0.1.0.3, the MSI `ProductVersion` is derived as `0.1.0` to satisfy Windows Installer version rules.
- Optional markdown file association
- Code signing

Recommended tooling:

- WiX Toolset

## macOS

Target:

- `.app` bundle
- DMG installer
- `osx-x64`
- `dragonmarkdown.icns` copied into `Contents/Resources`
- Developer ID signing
- Notarization

Recommended tooling:

- Native macOS runner
- Xcode command line tools
- `codesign`, `notarytool`, `hdiutil`

## Ubuntu And Mint

Target:

- DEB package
- `linux-x64` artifact: `dragonmarkdown_<version>_amd64.deb`
- `linux-arm64` artifact: `dragonmarkdown_<version>_arm64.deb`
- Desktop entry
- Icon
- MIME association for markdown files

Recommended tooling:

- `nfpm`

## RedHat Family

Target:

- RPM package
- `linux-x64` artifact: `dragonmarkdown-<version>.x86_64.rpm`
- `linux-arm64` artifact: `dragonmarkdown-<version>.aarch64.rpm`
- Desktop entry
- Icon
- MIME association for markdown files

Recommended tooling:

- `nfpm`

## Release Flow

1. Tag the release.
2. Native runners build platform artifacts for `win-x64`, `linux-x64`, `linux-arm64`, and `osx-x64`.
3. CI uploads installers.
4. The publish job downloads all artifacts and generates one final `SHA256SUMS.txt`.
5. Release notes list installer hashes and supported platforms.
