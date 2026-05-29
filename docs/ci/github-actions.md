# GitHub Actions

GitHub is the canonical home for DragonMarkdown:

```text
https://github.com/tmassey1979/DragonMarkdown.git
```

The repository uses GitHub Actions for validation, coverage, and release artifact publishing.

## Workflows

`.github/workflows/ci.yml`

- Runs on pull requests and pushes to `master` or `main`.
- Validates Windows, Ubuntu, and macOS.
- Builds in Release.
- Runs the full test suite.
- Uploads Cobertura coverage XML from the Ubuntu coverage job.
- Enforces the 80% package-level line coverage gate for testable app and core code.

`.github/workflows/release.yml`

- Runs on tags matching `v*` and manual dispatch.
- Builds installer artifacts:
  - Windows `win-x64` MSI
  - Linux `linux-x64` DEB/RPM
  - Linux `linux-arm64` DEB/RPM for Raspberry Pi 64-bit validation
  - macOS DMG for `osx-x64`
- Generates one final `SHA256SUMS.txt` in the publish job after all artifacts are downloaded.
- Creates a GitHub Release automatically for version tags.

## Runner Strategy

GitHub-hosted runners are enough for the current validation and publish artifact workflows:

| Platform | Runner |
| --- | --- |
| Windows | `windows-latest` |
| Ubuntu / Mint / Raspberry Pi 64-bit package target | `ubuntu-latest` |
| macOS Intel and Apple Silicon target | `macos-latest` |

Professional native installers use additional tooling:

- Windows MSI: WiX Toolset and code-signing certificate access.
- macOS DMG: Apple Developer ID certificate, notarization credentials, `codesign`, `notarytool`, and `hdiutil`.
- Linux DEB/RPM: `nfpm`, package metadata, desktop entry, icon, and MIME registration.

GitHub-hosted runners can build unsigned packages. Signed production installers should use GitHub Actions secrets or locked-down self-hosted runners for certificate access.

## Required Secrets For Future Signed Releases

These are not required for the current archive workflow, but they are the expected names for signed installer work:

| Secret | Purpose |
| --- | --- |
| `WINDOWS_CODESIGN_CERT_BASE64` | Windows signing certificate |
| `WINDOWS_CODESIGN_CERT_PASSWORD` | Windows signing certificate password |
| `APPLE_DEVELOPER_ID` | macOS Developer ID identity |
| `APPLE_APP_SPECIFIC_PASSWORD` | Notarization password or token |
| `APPLE_TEAM_ID` | Apple team ID |

## Local Parity

Before pushing, run:

```powershell
dotnet build .\DragonMarkdown.slnx
dotnet test .\DragonMarkdown.slnx --no-build
dotnet test .\DragonMarkdown.slnx --no-build --collect:"XPlat Code Coverage" --settings .\coverlet.runsettings --results-directory .\TestResults\Coverage
```

Before v0.1.0.4 release publication, also validate the final release payload with:

```powershell
.\build\release\validate-v0.1.0.4-artifacts.ps1 -ArtifactDir .\artifacts\installers
```
