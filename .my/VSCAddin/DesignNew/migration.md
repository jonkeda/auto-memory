# Migration plan

Mapping the redesign to the current code in
[vscode-extension/](../../../vscode-extension/) and proposing a milestone
breakdown so the work fits the existing step-file workflow under
`.my/VSCAddin/m*/`.

## What stays

| Asset | Status | Notes |
|---|---|---|
| `vscode-extension/src/extension.ts` activation | Keep | Adds new `AutoMemory: Glance/Find/Setup` commands |
| `AutoMemoryPanel` class structure | Keep | Stays the dual-mode webview host |
| `runner.ts` 60 s cache | Keep | Add file-watcher invalidation in a step |
| `messages.ts` `{type, payload}` envelope | Keep | New types: `glance`, `find`, `setup`, `navigate` |
| `panel.css` (file) | Replace contents | Token-based rewrite per [visual-language.md](visual-language.md) |
| `panel.js` | Replace | Tab router + per-view renderers |
| `html.ts` skeleton | Replace | Three sections become three tab panels |
| Existing JSON contract from CLI | Keep | The dashboard adapts; CLI does not change |

## What goes

| Asset | Replacement |
|---|---|
| `renderNoDb` block | Per-tab empty/error states |
| Top install-status header | Setup tab → Binary section |
| Inline `Refresh` button at bottom | Context-bar `⟳` icon |
| Standalone `Search` section | Find tab default state |
| `showError` 5 s red banner | Inline tab-local error block |
| Emoji zone icons | Codicons (`$(pass)`, `$(error)`, …) |
| `opacity: 0.6/0.7` muted text | `color: var(--vscode-descriptionForeground)` |

## What's new

| Surface | New code |
|---|---|
| Tab bar + context bar | `panel.js` tab router, repo chip listener |
| File watcher → cache invalidation | `runner.ts` (or new `watcher.ts`) |
| Session detail view (editor tab) | New `AutoMemorySessionDetailPanel` or a route inside the existing panel |
| Codicon stylesheet load | `html.ts` adds `<link>` for codicons css from VS Code's bundled file |
| Diagnostics report generator | `src/panel/diagnostics.ts` — gathers version + counts + errors and serializes to markdown |
| Settings keys | `package.json` `contributes.configuration` — `defaultTab`, `density`, `autoFilterByRepo`, `sources` |

## Suggested milestone breakdown

The existing convention is one milestone folder per phase, with
`step-NN-*.md` files. Sketch:

### M9 — Dashboard redesign foundation

- step-01 — Add tab router + context bar; ship Glance / Find / Setup
  shells with the existing data wiring (no behaviour change inside).
- step-02 — Token-based `panel.css`; remove emoji; add codicon stylesheet
  link.
- step-03 — Empty / loading / error state primitives shared by all tabs.
- step-04 — Repo chip + `autoFilterByRepo` setting + workspace-folder
  watcher.
- step-05 — Tests: webview render snapshot per tab, tab routing, empty
  states.

### M10 — Glance + health detail

- step-01 — Last-session card + Resume action.
- step-02 — Health bar + dimension rows with auto-expanded worst zone.
- step-03 — Health dimension detail card with `Open Copilot Chat` action.
- step-04 — `aria-live` Last-session updates + new-session highlight pulse.
- step-05 — Tests.

### M11 — Find + session detail

- step-01 — Search input with Enter submit; default empty-query lists
  recent sessions.
- step-02 — Source chips + `sources` setting persistence.
- step-03 — Result-row component with kebab menu (copy summary / copy id /
  reveal source).
- step-04 — Editor-tab session detail view (turns rendering, expand /
  collapse, copy markdown).
- step-05 — File-watcher refresh + virtualised list when count > 200.
- step-06 — Tests.

### M12 — Setup + diagnostics

- step-01 — Setup tab three-section layout.
- step-02 — Backends section reads from `health --json` and from M6 / M7
  detection logic.
- step-03 — Diagnostics expander + report generator + `Copy report` /
  `Open issue`.
- step-04 — Settings UI hooks (open VS Code settings, density toggle).
- step-05 — Tests.

## Per-step prerequisites

```
M9.01 ───┬── M9.02 ── M9.03 ── M9.04 ── M9.05
         │
         └── M10.01 ── M10.02 ── M10.03 ── M10.04 ── M10.05
         │
         └── M11.01 ── M11.02 ── M11.03 ── M11.04 ── M11.05 ── M11.06
         │
         └── M12.01 ── M12.02 ── M12.03 ── M12.04 ── M12.05
```

M10 / M11 / M12 can be implemented in parallel after M9.01 lands.

## Risk notes

- **Codicon stylesheet path.** VS Code does not expose a stable webview
  URI to its bundled codicons; the safe approach is to vendor
  `node_modules/@vscode/codicons/dist/codicon.css` + `codicon.ttf` into
  `vscode-extension/media/codicons/` and load them via
  `webview.asWebviewUri`. Validate this in M9.02 before relying on it.
- **Editor-tab session detail.** Reusing the same webview for Glance/Find/
  Setup *and* full session detail can be slow on large transcripts.
  Decision in M11.04: either a second webview panel class or virtualised
  rendering inside the existing one. Default to virtualised; second panel
  only if needed.
- **File watcher reliability.** VS Code's `chokidar`-backed watcher is
  flaky on Windows for files outside the workspace. The session-state and
  workspaceStorage paths are *outside* the workspace, so we may need a
  polling fallback (every 30 s while panel is visible) — verify in M11.05.
- **CSP and codicons.** Codicon CSS uses `font-face` referencing a
  `.ttf`. CSP `font-src 'self'` is required; current CSP omits `font-src`,
  so M9.02 must add `font-src ${webview.cspSource}`.

## Acceptance for the redesign as a whole

- All twelve use cases in [use-cases.md](use-cases.md) are reachable from
  the panel.
- No emoji in the rendered DOM (grep `panel.js` for emoji literals).
- Light, Dark, and High Contrast Aqua themes render every tab without
  hex-fallback colors visibly clashing.
- Keyboard-only flow can: open panel → switch tab → search → open detail
  → copy summary → close detail.
- Cold panel render is < 50 ms after the first payload arrives (existing
  60 s cache makes subsequent renders trivial).
- Existing `dotnet test` suite still passes; new TypeScript unit tests
  cover the tab router and the renderers.
