# Clean Architecture Refactor Plan

This plan tracks the structural cleanup requested before installer hardening.

## Goals

- One production class, record, enum, or interface per file.
- Componentized Avalonia UI.
- Thin code-behind.
- Behavior protected by tests.
- App and core coverage remain above 80 percent line coverage for testable logic.

## Current Hotspots

`src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs`

- Contains `MainWindowViewModel`
- Contains `OpenDocumentViewModel`
- Contains `WorkspaceNodeViewModel`
- Contains `WorkspaceNodeKind`

`src/DragonMarkdown.Core/Rendering/MarkdownRenderResult.cs`

- Contains render result records and related enums.

`src/DragonMarkdown.Core/Workspaces/WorkspaceItem.cs`

- Contains `WorkspaceItemKind` and `WorkspaceItem`.

`src/DragonMarkdown.App/MainWindow.axaml`

- Owns the full workbench layout.

## Refactor Slices

1. Split view model support types into separate files.
2. Split core records/enums into separate files.
3. Add app user controls for menu, header, workspace tree, editor pane, preview pane, and status bar.
4. Move `MainWindow.axaml` to composition-only layout.
5. Re-run app coverage with `coverlet.runsettings`.
6. Run the desktop app and inspect the visual layout after each UI slice.

## Guardrails

- Do not change product behavior during the refactor.
- Keep tests passing after each slice.
- Keep commits focused and reversible.
- Do not broaden installer work until the structural cleanup is complete.
