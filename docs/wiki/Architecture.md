# Architecture

DragonMarkdown is split into a desktop shell and a core library.

## Projects

`DragonMarkdown.App`

- Avalonia desktop shell
- Windows and dialogs
- App view models
- Preview host integration

`DragonMarkdown.Core`

- Markdown document state
- Workspace tree discovery
- Markdown rendering
- Word/PDF export
- Offline Mermaid export rendering

## Boundaries

The app project owns user interaction and platform UI concerns. It should not contain markdown parsing, export rendering, or workspace traversal logic when those can live in core.

The core project should stay UI-free and testable from unit tests.

## UI Composition Direction

The workbench should be split into focused controls:

- Menu and shell header
- Workspace tree pane
- Editor pane
- Preview pane
- Status bar
- Dialogs such as About

`MainWindow` should compose those controls and handle native window concerns only.

## Current Refactor Target

The next cleanup pass should split the remaining multi-type files and move workbench UI sections out of `MainWindow.axaml`.
