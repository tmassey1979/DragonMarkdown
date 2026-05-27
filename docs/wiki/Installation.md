# Installation

DragonMarkdown is currently packaged through CI publish artifacts. Native installers are planned for the release pipeline.

## Windows

Download the `DragonMarkdown-win-x64.zip` artifact from CI, extract it, and run:

```text
DragonMarkdown.App.exe
```

The planned Windows installer format is MSI.

## macOS

Download the `DragonMarkdown-<version>-osx-x64.dmg` release asset.

The planned macOS installer format is a signed and notarized DMG containing a `.app` bundle.

## Ubuntu And Linux Mint

Download the `DragonMarkdown-linux-x64.tar.gz` artifact from CI and run the app binary from the extracted folder.

The planned package format for Ubuntu and Mint is DEB.

## RedHat Family

The planned package format for RedHat-family distributions is RPM.

## Startup Folder

DragonMarkdown accepts a startup path:

```bash
DragonMarkdown.App /path/to/markdown-folder
```
