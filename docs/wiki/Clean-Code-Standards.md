# Clean Code Standards

DragonMarkdown should stay easy to change as it grows.

## File And Type Rules

- One production class per file.
- One production enum per file.
- One production record per file.
- One production interface per file.
- Private nested types are allowed only when the type is an implementation detail that should not be reused elsewhere.
- Test files may group closely related test fixtures when that keeps the test easier to read.

## Naming

- Use names that describe the domain behavior, not implementation mechanics.
- Avoid abbreviations unless they are standard in the domain.
- Commands should read like user actions.
- Boolean properties should read as true/false statements.

## App Architecture

Keep logic out of Avalonia code-behind.

Code-behind should be limited to:

- Native dialogs
- Window lifetime
- Control hosting
- View-only event bridges

Application behavior should live in:

- View models
- Core services
- Small platform services behind interfaces

## UI Componentization

The workbench should be composed from focused controls:

- `MainMenuView`
- `ShellHeaderView`
- `WorkspaceTreeView`
- `EditorPaneView`
- `PreviewPaneView`
- `StatusBarView`
- `AboutWindow`

`MainWindow` should compose these controls instead of owning all layout directly.

## Testing

- Core behavior should be unit-tested in `DragonMarkdown.Core.Tests`.
- View-model behavior should be unit-tested in `DragonMarkdown.App.Tests`.
- Avalonia XAML composition should have smoke tests or screenshot/UI tests when the UI stabilizes.
- Coverage exclusions should be explicit and narrow.

## Refactor Rule

Before moving behavior, add or preserve tests. Refactors should keep user-visible behavior unchanged unless the change is explicitly requested.
