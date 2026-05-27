# Roadmap

## Current Focus

1. Clean code and UI componentization.
2. GitHub Actions validation and release artifacts.
3. Professional native installers.
4. Export fidelity improvements.

## Clean Code And UI Refactor

- Split multi-type production files into one type per file.
- Move workspace node view models out of `MainWindowViewModel.cs`.
- Split the workbench XAML into focused Avalonia user controls.
- Keep code-behind thin and behavior in view models/services.
- Preserve app and core coverage above 80 percent.

## Installer Packaging

Windows:

- Add MSI packaging with WiX Toolset.
- Add Start Menu shortcut.
- Add uninstall support.
- Add optional markdown file association.
- Add signing hooks.

macOS:

- Create `.app` bundle layout.
- Create DMG packaging.
- Add signing and notarization hooks.

Linux:

- Add DEB packaging for Ubuntu and Mint.
- Add RPM packaging for RedHat-family distributions.
- Add desktop entry, app icon, and MIME association.

## Export Improvements

- Expand Mermaid export support beyond common graph and flowchart syntax.
- Improve Word export fidelity for charts, code, and tables.
- Add PDF export layout options.

## Documentation

- Publish GitHub wiki pages from `docs/wiki`.
- Add installer screenshots and release instructions.
- Add contributor setup docs.
