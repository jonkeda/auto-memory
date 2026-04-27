# VSCAddin — UI Design

This folder describes the visual + interaction design of the AutoMemory VS Code
extension panel. It is the canonical reference for what the dashboard looks
like, how it behaves across data sources, and which VS Code theme tokens it
consumes.

The design here documents the **current implementation** in
[vscode-extension/media/panel.js](../../../vscode-extension/media/panel.js),
[vscode-extension/media/panel.css](../../../vscode-extension/media/panel.css),
and [vscode-extension/src/panel/html.ts](../../../vscode-extension/src/panel/html.ts),
plus near-term design intent for milestones M7 / M8 surfaces.

## Files

| File | Scope |
|---|---|
| [dashboard-layout.md](dashboard-layout.md) | Overall panel structure, section order, sidebar vs tab surface, wireframes |
| [components.md](components.md) | Reusable atoms: banner, row, score, badge, button, instructions row |
| [states-and-flows.md](states-and-flows.md) | Data-source state matrix (SQLite / session-state / vscode-chat / none), message protocol, error/loading paths |
| [theme-tokens.md](theme-tokens.md) | VS Code CSS variables in use, fallbacks, contrast guidance |

## Design principles

1. **Native feel.** Use VS Code theme variables (`--vscode-*`) for every color
   and font. No hard-coded palette.
2. **One panel, two surfaces.** The same HTML/CSS/JS renders inside the sidebar
   view and inside a full editor tab. Layout adapts via the `data-surface`
   attribute on `<html>`.
3. **Data source is a banner, not a mode.** Whether sessions come from the
   Copilot CLI SQLite DB, the flat session-state folder (M6), or the VS Code
   Chat transcripts (M7), the panel renders the same five sections. Source is
   surfaced via badges and an info banner, never a separate screen.
4. **Always something useful.** When there is no SQLite DB, the panel falls
   back to flat-file health (M8) and a session list — never an empty state.
5. **Cheap to render, slow to refetch.** All sections read from the 60 s
   `runner` cache; only the explicit Refresh button bypasses it.
6. **Keyboard reachable.** Every interactive element is a real `<button>` or
   `<input>` so VS Code's built-in keyboard handling and screen-reader
   support work without extra ARIA scaffolding.

## Out of scope

- Marketing imagery, marketplace screenshots — see `.my/VSCAddin/image/`.
- Internals of the CLI (`session-recall`) JSON contract — see milestone step
  files (e.g. `m8-health-flat-file/step-03-health-command.md`).
