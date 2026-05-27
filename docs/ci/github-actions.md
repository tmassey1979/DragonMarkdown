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

`.github/workflows/release.yml`

- Runs on tags matching `v*` and manual dispatch.
- Publishes self-contained outputs for:
  - `win-x64`
  - `linux-x64`
  - `osx-x64`
  - `osx-arm64`
- Uploads zipped or tarred artifacts.

## Runner Strategy

GitHub-hosted runners are enough for the current validation and publish artifact workflows:

| Platform | Runner |
| --- | --- |
| Windows | `windows-latest` |
| Ubuntu / Mint target | `ubuntu-latest` |
| macOS Intel and Apple Silicon target | `macos-latest` |

Professional native installers will need additional tooling:

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
