# DragonMarkdown V1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a cross-platform Avalonia markdown editor/viewer with folder/file opening, tree navigation, tabbed editing, and CEF-backed rich preview.

**Architecture:** The app separates core document/workspace/renderer behavior from the Avalonia shell. Core services are covered by unit tests; the shell binds to view models and hosts the CEF preview behind a preview-host seam.

**Tech Stack:** .NET 10, Avalonia, Markdig, OutSystems CefGlue, xUnit, FluentAssertions, CommunityToolkit.Mvvm.

---

### Task 1: Solution Bootstrap

**Files:**
- Create: `DragonMarkdown.slnx`
- Create: `src/DragonMarkdown.Core/DragonMarkdown.Core.csproj`
- Create: `src/DragonMarkdown.App/DragonMarkdown.App.csproj`
- Create: `tests/DragonMarkdown.Core.Tests/DragonMarkdown.Core.Tests.csproj`
- Create: `.gitignore`
- Create: `NuGet.Config`

- [ ] Create the solution and projects with .NET 10.
- [ ] Add package references for Avalonia, Markdig, CefGlue, xUnit, and FluentAssertions.
- [ ] Run `dotnet restore DragonMarkdown.slnx`.
- [ ] Run `dotnet build DragonMarkdown.slnx`.

### Task 2: Workspace Tree

**Files:**
- Create: `src/DragonMarkdown.Core/Workspaces/WorkspaceItem.cs`
- Create: `src/DragonMarkdown.Core/Workspaces/WorkspaceTreeBuilder.cs`
- Test: `tests/DragonMarkdown.Core.Tests/Workspaces/WorkspaceTreeBuilderTests.cs`

- [ ] Write a failing test proving generated folders are skipped and markdown/assets are included.
- [ ] Implement `WorkspaceTreeBuilder`.
- [ ] Run focused workspace tests.

### Task 3: Document State

**Files:**
- Create: `src/DragonMarkdown.Core/Documents/MarkdownDocument.cs`
- Create: `src/DragonMarkdown.Core/Documents/DocumentWorkspace.cs`
- Test: `tests/DragonMarkdown.Core.Tests/Documents/DocumentWorkspaceTests.cs`

- [ ] Write failing tests for opening, dirty tracking, saving, and close decisions.
- [ ] Implement document models and workspace operations.
- [ ] Run focused document tests.

### Task 4: Markdown Rendering

**Files:**
- Create: `src/DragonMarkdown.Core/Rendering/MarkdownRenderOptions.cs`
- Create: `src/DragonMarkdown.Core/Rendering/MarkdownRenderer.cs`
- Test: `tests/DragonMarkdown.Core.Tests/Rendering/MarkdownRendererTests.cs`

- [ ] Write failing tests for table/task/footnote rendering, Mermaid fences, MathJax preservation, and blocked outside-workspace assets.
- [ ] Implement Markdig rendering and HTML shell generation.
- [ ] Run focused rendering tests.

### Task 5: Avalonia Workbench

**Files:**
- Create: `src/DragonMarkdown.App/App.axaml`
- Create: `src/DragonMarkdown.App/App.axaml.cs`
- Create: `src/DragonMarkdown.App/Program.cs`
- Create: `src/DragonMarkdown.App/MainWindow.axaml`
- Create: `src/DragonMarkdown.App/MainWindow.axaml.cs`
- Create: `src/DragonMarkdown.App/ViewModels/MainWindowViewModel.cs`
- Create: `src/DragonMarkdown.App/Preview/IPreviewHost.cs`
- Create: `src/DragonMarkdown.App/Preview/CefPreviewHost.cs`

- [ ] Build the three-pane workbench with toolbar, tree, tabs, editor, preview pane, and status bar.
- [ ] Bind hide/show editor and preview commands.
- [ ] Wire view model commands for open, save, save all, create, rename, delete, and reveal.
- [ ] Host generated HTML in CEF.

### Task 6: Verification

**Files:**
- Create: `samples/DragonMarkdownDemo/README.md`

- [ ] Add a sample markdown folder with heading, table, task list, code fence, Mermaid diagram, MathJax block, and relative image reference.
- [ ] Run `dotnet test DragonMarkdown.slnx`.
- [ ] Run `dotnet build DragonMarkdown.slnx`.
- [ ] Launch the app and verify the workbench opens.
