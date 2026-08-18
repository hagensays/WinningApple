# Development Workflow

This repository uses:

branch → implementation → PR → CI → merge → build → release → verification

## Rules for Coding Agents

For every code change:

1. Read the current `AGENTS.md` first.
2. Read `SUITE_STANDARD.md` and `DESIGN_SYSTEM.md` before changing user-facing UI.
3. Treat current `main` as the authoritative source.
4. Never develop or commit directly on `main`.
5. Create a version branch from current `main` named exactly `vX.Y.Z`.
6. Implement the requested change on that branch.
7. Open a PR from `vX.Y.Z` into `main`.
8. Run CI on the PR.
9. Never bypass, disable or weaken failing CI.
10. If CI fails, inspect the actual failure, fix it on the branch and rerun CI.
11. Merge only after required CI checks pass.
12. After merge, automatically create/tag `vX.Y.Z`, build the release, create a GitHub Release, attach compiled artifacts, include short release notes, verify assets and delete the version branch.
13. A task is not finished until `branch → change → PR → green CI → merge → release → verification → cleanup` is complete.

## Repository Roles

This repository intentionally has two application projects:

- `src/HSTemplate` is the canonical source template copied into new product repositories.
- `src/HSSuite` is the optional downloadable launcher/home application.

The launcher is a product, not a shared library. Do not move common behavior into a runtime DLL that other HS apps must load.

## Template ↔ Launcher Sync Check

Every HSSuite change must explicitly consider whether the template and launcher need to stay aligned.

- When changing `src/HSTemplate`, always evaluate whether the same universal shell, design-system or launcher-contract change also belongs in `src/HSSuite`.
- When changing `src/HSSuite`, always evaluate whether the corresponding reusable behavior or visual rule should also be reflected in `src/HSTemplate` for future HS apps.
- Apply shared changes to both projects in the same version branch/PR when they are genuinely universal.
- Do not blindly mirror product-specific workspace or launcher-only behavior into the template.
- Changes to `DESIGN_SYSTEM.md`, `SUITE_STANDARD.md`, launcher discovery/Home behavior, bootstrap logic, CI or release behavior must also trigger this sync evaluation where relevant.
- Every PR in this repository should state the sync result: both projects updated, or one intentionally left unchanged with a short reason.
- Existing independent product repositories are not automatically modified by this check. If a template change should later be retrofitted to an existing HS app, record or propose that follow-up rather than touching another repo unless the user requested it.

## Suite Behaviour

- Preserve the HSSuite visual language unless a product requirement genuinely requires an exception.
- Use WPF and .NET Framework 4.7.2 by default for compatibility with the target office environment.
- Do not add NuGet packages, external runtimes, installers or framework dependencies unless the user explicitly approves the exception.
- Keep each application independently buildable and releasable.
- Do not introduce a shared HSSuite DLL. Reuse the template source instead.
- App-generated outputs default to the executable directory and must use non-overwriting names.
- Do not use `%TEMP%`, `%APPDATA%` or `%LOCALAPPDATA%` for application state/output by default.
- Product-specific source-file mutations are neither universally forbidden nor universally allowed; define them explicitly in the product repo.
- Keep changes focused on the requested task.
- Do not claim runtime testing when only compilation or CI testing occurred.
- If an important product/design decision is genuinely ambiguous, ask before choosing it.

## Optional Launcher Contract

- Every generated HS app must continue to work with no HSSuite launcher present.
- The `[ HS ]` header tile may open a sibling `HSSuite.exe` or semantic-version release executable such as `HSSuite-v0.2.0.exe`.
- When no launcher is present, the `[ HS ]` tile stays visually normal and must not show an error merely because HSSuite is absent.
- HSSuite scans only its own directory for `HS*.exe`; never recurse into subfolders.
- HSSuite must exclude itself and other HSSuite launcher variants from its app list.
- Do not use the registry, network access, telemetry, services, IPC daemons or shared application state for launcher discovery.
- Launch sibling apps as independent processes with their own working directory. HSSuite does not own their lifetime.
- `scripts/Initialize-App.ps1` must remove the HSSuite launcher project/solution when converting this template repository into a product repository.

## Naming

- Product repositories and executables should normally use the `HS` prefix: `HSScanner`, `HSRenamer`, `HSCompare`, etc.
- Release branches: `vMAJOR.MINOR.PATCH`.
- Public release asset: `<AppName>-vMAJOR.MINOR.PATCH.exe`.
