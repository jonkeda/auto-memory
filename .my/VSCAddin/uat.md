# VS Code Extension — User Acceptance Tests

Manual UAT checklist for the `auto-memory` VS Code extension. Run end-to-end before tagging a release.

## Pre-test setup

- [ ] Clean install: uninstall any prior version (`code --uninstall-extension auto-memory.auto-memory`)
- [ ] `vscode-extension/bin/<platform>/session-recall[.exe]` exists for current platform
- [ ] `npm run package` produced a `.vsix` in `vscode-extension/dist/`
- [ ] Install the VSIX: `code --install-extension dist/auto-memory-*.vsix`
- [ ] A real `~/.copilot/session-store.db` is populated with at least 5 sessions (or set `autoMemory.dbPath` to a fixture)
- [ ] Open a workspace folder (most tests need one)

## Tests

### Activation & UI chrome

- [X] **TC-01 Activation** — Reload window after install → extension activates without errors in `Help → Toggle Developer Tools → Console`
- [X] **TC-02 Status bar** — Right side of status bar shows `$(database) auto-memory`
- [X] **TC-03 Status bar click** — Clicking status bar item opens dashboard in editor tab
- [X] **TC-04 Activity Bar** — Clicking the database icon opens sidebar view "Dashboard"
- [ ] **TC-05 Command Palette** — Typing `Auto Memory:` lists all 7 commands (openDashboard, installBinary, updateBinary, uninstall, addInstructions, runHealth, refreshSidebar)
- [ ] **TC-26 Settings schema** — Opening Settings and searching "auto memory" shows all 5 settings with descriptions and defaults

### Binary install & PATH

- [X] **TC-06 Install** — `Auto Memory: Install CLI` copies binary to `installDir()`; success notification shown
- [ ] **TC-07 PATH (userPath)** — After install, `session-recall --version` works in a new external terminal (Windows: new shell required after `setx`)
- [ ] **TC-08 PATH (vscodePath)** — Setting `autoMemory.pathStrategy` to `vscodePath` and reinstalling makes `session-recall` available in the VS Code integrated terminal
- [ ] **TC-09 Update no-op** — `Auto Memory: Update CLI` immediately after install shows "Already up to date"
- [ ] **TC-10 Auto-update prompt** — Replacing `installDir/session-recall` with an older binary and reloading triggers an update notification
- [ ] **TC-30 Uninstall** — `Auto Memory: Uninstall` shows modal confirm, removes `installDir()`, shows notification

### Dashboard

- [X] **TC-11 Install status** — Dashboard header shows version, install dir, and PATH strategy
- [ ] **TC-12 Health** — Health section renders all 9 dimensions with zone icons; overall score shown in colour
- [ ] **TC-13 Sessions** — Sessions section lists 10 most recent with date/repo/summary
- [ ] **TC-14 Session detail** — Clicking a session row shows a detail pane inline (editor-tab surface)
- [ ] **TC-15 Search** — Entering a known query and clicking Go renders result cards; empty query shows warning text (no crash)
- [ ] **TC-16 Refresh** — Clicking Refresh reloads health/sessions/install status and invalidates cache

### Copilot instructions

- [ ] **TC-17 Strategy A add** — Clicking `[A] Add` in dashboard creates/appends `~/.copilot/copilot-instructions.md`; badge → ✅ Installed
- [ ] **TC-18 Strategy A idempotent** — Clicking `[A] Add` again shows "already installed"; file not duplicated
- [ ] **TC-19 Strategy B add** — Clicking `[B] Add` creates `<root>/.github/instructions/session-recall.instructions.md` with frontmatter
- [ ] **TC-20 Strategy C add** — Clicking `[C] Add` adds pointer to `settings.json` `github.copilot.chat.codeGeneration.instructions`
- [ ] **TC-21 Remove** — Clicking `[A] Remove` strips block; badge → ⚠️ Not installed; surrounding content intact
- [ ] **TC-22 Multi-root** — With a multi-root workspace, clicking `[B] Add` prompts a Quick Pick; selection writes to that folder only
- [ ] **TC-23 No workspace** — With no folder open, `[B]` and `[C]` buttons are disabled with tooltip "Open a workspace folder first"

### Theming & error handling

- [ ] **TC-24 Dark theme** — Switching to a dark theme and reopening dashboard: colours adapt, text readable
- [ ] **TC-25 Light theme** — Switching to a light theme and reopening dashboard: colours adapt, text readable
- [ ] **TC-27 DB missing** — Setting `autoMemory.dbPath` to nonexistent path and reloading: error banner shown in webview, extension does not crash
- [ ] **TC-28 CSP compliance** — Webview devtools console shows no CSP violation errors (`Cmd/Ctrl+Shift+P → Webview Developer Tools`)

### Platform

- [ ] **TC-29 WSL** *(skip if not applicable)* — Opening a WSL Remote window and running `Auto Memory: Install CLI` installs Linux binary to `~/.local/bin/`; works from WSL terminal

## Result summary

- Total: 30
- Passed: __
- Failed: __
- Skipped (platform N/A): __

## Sign-off

- [ ] All P0 tests passed (TC-01..TC-06, TC-11..TC-13, TC-17..TC-19, TC-26, TC-30)
- [ ] No CSP violations or unhandled promise rejections in devtools console
- [ ] Tester: ____________________   Date: ____________   Version: ____________
