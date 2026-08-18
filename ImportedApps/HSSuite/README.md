# HSSuite

Canonical template, design standard and optional launcher for small HS Windows desktop utilities.

HSSuite still follows the same core rule: every HS tool is an independent application with its own repository, executable, version history, CI and releases. The downloadable `HSSuite` application is only a convenience home screen. No HS app requires it to run.

## Optional launcher

Place HSSuite beside other HS executables:

```text
HS Tools\
├── HSSuite.exe
├── HSScanner.exe
├── HSPdf.exe
└── HSWhatever.exe
```

HSSuite scans only its own directory for sibling `HS*.exe` files and shows them as launchable tiles. It does not recurse into subfolders, use the registry, create shared state, add a service, or introduce a shared runtime dependency.

Apps created from the current template use the `[ HS ]` tile in the top-left as an optional Home button:

- if `HSSuite.exe` is beside the app, clicking `[ HS ]` opens it;
- versioned release assets such as `HSSuite-v0.2.0.exe` are also recognized;
- if no HSSuite launcher is present, the tile remains the normal suite mark and the product works unchanged.

## Repository structure

- `src/HSTemplate/` — canonical starter source for new independent HS apps
- `src/HSSuite/` — the optional downloadable launcher itself
- `HSTemplate.sln` — template build
- `HSSuite.sln` — launcher build

The launcher is a product inside the template repository, not a library consumed by other apps.

## Intended environment

- Windows 10 Enterprise/LTSC-class office machines
- WPF on .NET Framework 4.7.2
- no WinUI 3 requirement
- no NuGet/runtime dependencies by default
- self-contained application folders
- generated output defaults to the folder containing the running EXE
- generated files must not overwrite existing files

## New app flow

1. Create a new repository from this GitHub template.
2. Run `scripts/Initialize-App.ps1 -AppName HSWhatever -Description "..."`, or have the coding agent perform the equivalent transformation.
3. The bootstrap removes the HSSuite launcher project and keeps the standalone app template.
4. Read `AGENTS.md`, `SUITE_STANDARD.md` and `DESIGN_SYSTEM.md` before implementation.
5. Build the actual product on a semantic-version branch such as `v0.1.0`.
6. PR → CI → merge → automatic release → verification → cleanup.

## Template contents

- a compilable WPF starter shell
- canonical colors, spacing and control styles
- standard header, workspace and status bar
- optional `[ HS ]` → HSSuite Home behavior with no hard dependency
- local-output/non-overwrite helper
- universal suite rules
- CI and release workflows
- template validation checks
- initialization script

## Important distinction

Universal suite rules belong here. Product-specific rules belong in the individual application repo. HSSuite may coordinate launching, but it must never become required infrastructure for HSScanner, HSPdf or future tools.

See `TEMPLATE_USAGE.md` for the exact creation workflow.
