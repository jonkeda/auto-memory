# M1 — Scaffold

**Goal:** TypeScript VS Code extension scaffold under `vscode-extension/` with command palette, status bar, settings, and Activity Bar sidebar contributions wired up — even if the underlying actions are stubs.

## Source of truth

- Roadmap: `.my/VSCAddin/roadmap-vscode.md`
- Milestone: `.my/VSCAddin/m1-scaffold/milestone.md`

## Steps

| # | File | Title |
|---|---|---|
| 01 | `step-01-npm-tsconfig.md` | npm init, tsconfig, esbuild, eslint, .vscodeignore |
| 02 | `step-02-package-json.md` | package.json contributions: commands, viewsContainers, views, configuration |
| 03 | `step-03-extension-entry.md` | `src/extension.ts` activate/deactivate; register command stubs |
| 04 | `src/status-bar.ts` | Status bar item with click → openDashboard |
| 05 | `step-05-sidebar-provider.ts` | `SidebarProvider` (WebviewViewProvider) — placeholder HTML |
| 06 | `step-06-launch-config.md` | `.vscode/launch.json` for Extension Development Host |

## Acceptance

- F5 launches Extension Development Host
- Activity Bar shows `auto-memory` icon → opens sidebar with placeholder
- Command Palette lists all registered `auto-memory.*` commands
- Status bar shows `$(database) auto-memory`
- `npm run build` succeeds with zero TypeScript or ESLint errors
