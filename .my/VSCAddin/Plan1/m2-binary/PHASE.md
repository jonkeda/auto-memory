# M2 — Binary

**Goal:** `BinaryManager` service detects, installs, updates, and uninstalls the `session-recall` CLI per platform; PATH strategies wired up; WSL detection works.

## Source of truth
- Milestone: `.my/VSCAddin/m2-binary/milestone.md`

## Steps

| # | File | Title |
|---|---|---|
| 01 | `step-01-binary-bundling.md` | Bundle per-platform binaries under `bin/` |
| 02 | `step-02-binary-manager.md`  | `BinaryManager` service — detect, version, paths |
| 03 | `step-03-install-command.md` | `auto-memory.installBinary` command + extract logic |
| 04 | `step-04-path-strategies.md` | userPath / vscodePath / manual implementations |
| 05 | `step-05-wsl-support.md`     | Detect WSL remote, install to `/usr/local/bin` |
| 06 | `step-06-update-uninstall.md`| `updateBinary` + `uninstall` commands |

## Acceptance
- All M2 milestone acceptance criteria met
