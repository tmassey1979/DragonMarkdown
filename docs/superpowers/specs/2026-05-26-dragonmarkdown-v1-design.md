# DragonMarkdown V1 Design

## Goal

DragonMarkdown is a cross-platform C# desktop markdown editor and viewer for working with either a folder of documentation or a single markdown file. The default workflow opens a folder and presents a balanced workbench with a file tree, tabbed editor, and rich preview.

## Approved Direction

The app will use Avalonia for the native desktop shell and an embedded Chromium/CEF preview surface for markdown features that require browser-grade rendering. The renderer will use Markdig for markdown-to-HTML conversion and will ship a built-in v1 feature set instead of exposing extension configuration.

## User Experience

The default window uses three panes:

- Left: workspace tree rooted at the opened folder.
- Center: tabbed markdown editor with dirty indicators.
- Right: rendered preview.

Users can hide or restore the editor and preview independently. The tree remains the navigation anchor unless the app is in single-file mode.

## Workspace Modes

Folder mode is the default. The tree shows markdown files and supporting assets while filtering common generated folders such as `bin`, `obj`, `.git`, `.vs`, `.idea`, `node_modules`, and `.superpowers`.

Single-file mode opens one markdown file directly. The app should still understand the file's containing directory as the base for relative links and images.

## File Operations

V1 includes basic file management:

- Open folder.
- Open file.
- Create markdown file.
- Create folder.
- Rename file or folder.
- Delete with confirmation.
- Reveal in the operating system file manager.
- Save active file.
- Save all dirty files.

## Editor

The editor is a plain markdown text editor in v1. It must support multiple open files as tabs, track dirty state, prompt before closing unsaved changes, and update preview content from the active document.

## Markdown Preview

The v1 preview pipeline supports:

- CommonMark and GitHub-flavored markdown basics.
- Tables.
- Task lists.
- Footnotes.
- YAML front matter handling.
- Raw HTML in trusted workspaces.
- Fenced code blocks with syntax highlighting.
- Mermaid diagrams.
- MathJax inline and block math.
- Relative images and links constrained to the workspace root.

Opened folders are treated as trusted workspaces by default. Local asset resolution must stay inside the selected workspace root so a markdown file cannot reference arbitrary local files outside the workspace.

## Architecture

The solution is split into focused units:

- `DragonMarkdown.Core`: workspace, document, file-tree, and markdown-rendering domain services.
- `DragonMarkdown.App`: Avalonia UI shell and preview host integration.
- `DragonMarkdown.Core.Tests`: behavior tests for workspace scanning, document state, path safety, and markdown rendering.

The preview host is behind an app-level seam so the renderer can feed generated HTML to CEF now and another browser control later if packaging requirements change.

## Error Handling

File-system failures should surface as explicit status messages and leave existing editor state untouched. Unsafe relative asset paths are blocked during HTML generation. Closing dirty tabs requires an explicit save, discard, or cancel decision.

## Testing

Core behavior is tested first with fast unit tests. UI work is validated by build/run checks and focused view model tests where practical. Preview output tests verify that markdown extensions produce expected HTML markers for Mermaid, MathJax, code highlighting hooks, and workspace-relative asset URLs.

## Implementation Slices

1. Bootstrap the .NET solution, Avalonia app, core library, tests, NuGet config, and static three-pane shell.
2. Add workspace scanning and file-tree models.
3. Add tabbed document state, dirty tracking, save, and close decisions.
4. Add Markdig HTML rendering with trusted workspace asset rewriting.
5. Add CEF preview host integration and generated HTML shell with Mermaid, MathJax, and syntax highlighting scripts.
6. Add file-management commands and visual polish.
