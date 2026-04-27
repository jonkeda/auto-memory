# DesignNew — AutoMemory dashboard, redesigned

A reset of the VS Code extension UI, driven by use cases instead of by the
shape of the current `panel.html` skeleton.

## Why redesign

The current panel ([../Design/dashboard-layout.md](../Design/dashboard-layout.md)) is a
single flat scroll: header → Health → Sessions → Search → Instructions →
Refresh. That works for a quick glance but breaks down when:

- The sidebar is narrow (≈ 320 px); the user has to scroll past Health to
  reach Search.
- A session row is clicked — there is no detail view.
- A health dimension is RED — there is no actionable hint surfaced.
- Three backends (SQLite / session-state / vscode-chat) coexist — the only
  signal is a small textual badge.
- The user wants only sessions for the repo they currently have open —
  there is no filter affordance.
- A search returns 50 results — there is no way to refine or open one.

## Files

| File | Scope |
|---|---|
| [use-cases.md](use-cases.md) | 12 prioritised user goals with triggers and success criteria |
| [information-architecture.md](information-architecture.md) | Top-level navigation, view hierarchy, what lives where |
| [wireframes.md](wireframes.md) | ASCII wireframes per view, sidebar + tab variants |
| [interactions.md](interactions.md) | Flows, transitions, keyboard shortcuts, refresh model |
| [visual-language.md](visual-language.md) | Theme tokens, codicons (no emoji), density tiers, motion |
| [migration.md](migration.md) | What changes vs. the current panel; suggested milestone breakdown |
| [copilot-chat-integration.md](copilot-chat-integration.md) | How AutoMemory plugs *into* the Copilot Chat panel: `#recall` LM tool, `@autoMemory` participant, slash commands, inline-chat rendering |

## Design principles

1. **Goals over sections.** The sidebar's top-level nav reflects what users
   want to do (Glance, Find, Open, Setup), not which CLI command produced
   the data.
2. **One data model, many sources.** SQLite, session-state, and vscode-chat
   are merged into a single `Session` list with a `source` chip; filters
   never force the user to think in backends.
3. **Every red light has a green button.** RED/AMBER health dimensions
   render an actionable card with a `Fix` or `Learn more` button — never a
   dead end.
4. **Sidebar is a launcher, tab is a workbench.** Heavy content (full
   transcripts, charts, multi-column session tables) lives only in the
   editor-tab surface. The sidebar surfaces summary + entry points.
5. **Repo-aware by default.** When a workspace is open, lists are
   pre-filtered to that repo with a clearly removable chip.
6. **No emoji in core UI.** Use VS Code codicons so themes and screen
   readers behave correctly.
7. **Keyboard first.** Every view reachable via a Command Palette command
   and a keybinding; Enter submits in every text input.
8. **Cheap by default, fresh on demand.** 60 s cache stays. Auto-refresh
   triggers on workspace folder change and on a host-side file watcher
   firing in the session-state directory.

## Non-goals

- A new color palette. Themes are still 100 % VS Code variables.
- Cross-extension features (e.g. integrating with GitLens). Out of scope.
- Editing or deleting sessions. The dashboard remains read-only over the
  CLI's data.
- A login / cloud sync layer. AutoMemory stays local-first.
