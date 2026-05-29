# DragonMarkdown

DragonMarkdown is a cross-platform desktop markdown editor and viewer written in C# with Avalonia and .NET. It is designed for folder-based writing projects, live preview, rich markdown extensions, and clean exports to Word and PDF.

## Features

- Open a folder or a single markdown file.
- Browse workspace files in a tree view.
- Edit markdown and preview rendered output side by side.
- Hide the editor or preview pane and automatically expand the other pane.
- Render advanced markdown through Markdig, including tables, task lists, footnotes, YAML front matter, MathJax, Prism highlighting, and Mermaid preview support.
- Export the active document to Word or PDF with page setup, header/footer options, validation, and batch PDF export.
- Render common Mermaid `graph` and `flowchart` fences into export diagrams.
- Analyze docs health for broken links, missing images, duplicate anchors, orphan documents, dead assets, and unsupported export diagrams.
- Use writing helpers for generated tables of contents, tables, Mermaid diagrams, image markdown, backlinks, active document statistics, workspace statistics, and git workspace status.
- Open packaged help and About screens from the Help menu.

## Requirements

- .NET SDK 10.0 or newer.
- Windows, macOS, or Linux desktop environment supported by Avalonia.

## Build

```powershell
dotnet restore .\DragonMarkdown.slnx
dotnet build .\DragonMarkdown.slnx
```

## Test

```powershell
dotnet test .\DragonMarkdown.slnx --no-build
```

Coverage:

```powershell
dotnet test .\DragonMarkdown.slnx --no-build --collect:"XPlat Code Coverage" --settings .\coverlet.runsettings --results-directory .\TestResults\Coverage
```

Current coverage target:

- `DragonMarkdown.App`: at least 80 percent line coverage for testable app logic.
- `DragonMarkdown.Core`: at least 80 percent line coverage.

## Run

```powershell
dotnet run --project .\src\DragonMarkdown.App\DragonMarkdown.App.csproj
```

Open a folder at startup:

```powershell
dotnet run --project .\src\DragonMarkdown.App\DragonMarkdown.App.csproj -- C:\docs\my-project
```

## Publish

Self-contained publish examples:

```powershell
dotnet publish .\src\DragonMarkdown.App\DragonMarkdown.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64
dotnet publish .\src\DragonMarkdown.App\DragonMarkdown.App.csproj -c Release -r linux-x64 --self-contained true -o .\artifacts\publish\linux-x64
dotnet publish .\src\DragonMarkdown.App\DragonMarkdown.App.csproj -c Release -r linux-arm64 --self-contained true -o .\artifacts\publish\linux-arm64
dotnet publish .\src\DragonMarkdown.App\DragonMarkdown.App.csproj -c Release -r osx-x64 --self-contained true -o .\artifacts\publish\osx-x64
```

Installer packaging is tracked in the release docs and GitHub Actions setup. Native installers should be produced on native runners: Windows for MSI, macOS for DMG/signing/notarization, and Linux for DEB/RPM. The v0.1.0.4 release keeps the public four-part tag and display version while deriving the Windows MSI `ProductVersion` from the first three parts, because MSI package versions are three-part values.

## Repository Layout

```text
src/
  DragonMarkdown.App/      Avalonia desktop shell and app view models
  DragonMarkdown.Core/     Markdown documents, workspace tree, rendering, exports
tests/
  DragonMarkdown.App.Tests/
  DragonMarkdown.Core.Tests/
docs/
  ci/                      Runner and pipeline setup
  wiki/                    Source pages for the GitHub wiki
```

## Engineering Standards

- Keep one production type per file unless a private nested type is intentionally scoped to an implementation.
- Keep UI composition split into focused controls instead of growing `MainWindow`.
- Keep domain logic out of Avalonia code-behind.
- Protect behavior with tests before refactors.
- Preserve coverage for app view models and core services; exclude only UI shell composition files from unit coverage.

## CI

GitHub is the canonical home for DragonMarkdown:

```text
https://github.com/tmassey1979/DragonMarkdown.git
```

The repository includes GitHub Actions workflows for build, test, coverage, and release publish artifacts across Windows, Ubuntu, and macOS.

See [docs/ci/github-actions.md](docs/ci/github-actions.md).

## Wiki

Wiki source pages live in [docs/wiki](docs/wiki). They are written as GitHub Wiki-compatible markdown pages and can be pushed to the separate `DragonMarkdown.wiki.git` repository.

See [docs/wiki/README.md](docs/wiki/README.md).
