# Development Workflow

This repository uses:

branch → implementation → PR → CI → merge → build → release → verification

## Rules for Coding Agents

For every code change:

1. Read the current `AGENTS.md` first.
2. Treat current `main` as the authoritative source.
3. Never develop or commit directly on `main`.
4. Create a version branch from current `main`:
   `vX.Y.Z`
5. Implement the requested change on that branch.
6. Open a PR from `vX.Y.Z` into `main`.
7. Run CI on the PR.
8. Never bypass, disable, or weaken failing CI.
9. If CI fails, inspect the actual failure, fix it on the branch, and rerun CI.
10. Merge only after required CI checks pass.
11. After merge, automatically:
    - create/tag `vX.Y.Z`
    - build the release
    - create a GitHub Release
    - attach the compiled application/artifacts
    - include short release notes
12. Verify the release workflow succeeded and the expected release assets exist.
13. Delete the version branch after a successful release.
14. A task is not finished until:
    `branch → change → PR → green CI → merge → release → verification → cleanup`

## Agent Behaviour

- Follow the existing project structure and conventions unless there is a reason to change them.
- Keep changes focused on the requested task.
- Do not make unrelated repository-wide changes.
- Do not claim something was runtime-tested when it was only compiled or tested in CI.
- If an important product/design decision is genuinely ambiguous, ask before choosing it.

## Versioning

Release branches use:

`vMAJOR.MINOR.PATCH`

Example:

`v0.1.1`
