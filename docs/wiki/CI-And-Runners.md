# CI And GitHub Actions

DragonMarkdown uses GitHub Actions. The GitHub repository is the canonical project home:

```text
https://github.com/tmassey1979/DragonMarkdown.git
```

## What CI Does

- Restores dependencies
- Builds in Release
- Runs the full test suite
- Collects coverage on Linux
- Publishes self-contained app artifacts for Windows, Linux, and macOS

## GitHub-Hosted Runners

The current workflows use:

| Platform | Runner |
| --- | --- |
| Windows | `windows-latest` |
| Ubuntu / Mint target | `ubuntu-latest` |
| macOS | `macos-latest` |

## Workflows

- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`

Detailed setup lives in:

```text
docs/ci/github-actions.md
```
