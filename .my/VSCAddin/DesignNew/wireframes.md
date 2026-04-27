# Wireframes

ASCII wireframes for each tab on each surface. Sidebar wireframes assume
~320 px; editor-tab wireframes assume ≥ 800 px. Spacing and pixel sizes
appear in [visual-language.md](visual-language.md).

Codicons are noted as `$(name)`; emoji never appear in core UI.

---

## Sidebar — Glance (default tab)

```
┌──────────────────────────────────────────────┐
│ $(repo) repo-A  ✕               $(refresh) ⋯ │
├──────────────────────────────────────────────┤
│   Glance     Find      Setup                 │
│   ─────────                                  │
├──────────────────────────────────────────────┤
│ Last session                                 │
│ ┌──────────────────────────────────────────┐ │
│ │ Apr 26 14:02   $(comment) chat           │ │
│ │ Refactor TranscriptReader to use         │ │
│ │ File.ReadLines for streaming…            │ │
│ │ [ Resume ]                               │ │
│ └──────────────────────────────────────────┘ │
│                                              │
│ Health                              8.4 / 10 │
│ ┌──────────────────────────────────────────┐ │
│ │ ████████████████████░░       (full bar)  │ │
│ └──────────────────────────────────────────┘ │
│ $(pass)  Freshness            10.0     ›     │
│ $(pass)  Corpus Size          10.0     ›     │
│ $(error) Repo Coverage         2.0     ▾     │
│   ┌────────────────────────────────────────┐ │
│   │ Threshold: ≥ 5 sessions for repo       │ │
│   │ Found: 0 sessions for repo-A           │ │
│   │ [ Open Copilot Chat ]                  │ │
│   └────────────────────────────────────────┘ │
│ $(pass)  Summary Coverage     10.0     ›     │
│ $(pass)  Recent Activity      10.0     ›     │
└──────────────────────────────────────────────┘
```

Notes:

- The repo chip is the first thing the user sees — confirms scope.
- The worst dimension (RED here) is auto-expanded; the others are
  collapsed.

---

## Sidebar — Find

```
┌──────────────────────────────────────────────┐
│ $(repo) repo-A  ✕               $(refresh) ⋯ │
├──────────────────────────────────────────────┤
│   Glance     Find      Setup                 │
│             ──────                           │
├──────────────────────────────────────────────┤
│ ┌──────────────────────────────────┐ [ Go ]  │
│ │ $(search) react hooks            │         │
│ └──────────────────────────────────┘         │
│ Sources:  ◉ SQLite  ◉ State  ◉ Chat          │
│                                              │
│ Apr 26  repo-A  $(comment) chat              │
│  Refactor TranscriptReader to…       $(more) │
│ Apr 26  repo-A  $(database) state            │
│  M8 step 03 implementation           $(more) │
│ Apr 25  repo-A  $(comment) chat              │
│  Add Enter-key search handler        $(more) │
│ …                                            │
│                                              │
│ Showing 12 of 86 (filtered)                  │
└──────────────────────────────────────────────┘
```

When the input is empty, the list defaults to recent sessions for the
current filter — Find doubles as the recent-sessions view.

Source chips use a filled radio when active and a hollow one when off; the
text remains readable in both states.

---

## Sidebar — Setup

```
┌──────────────────────────────────────────────┐
│ $(repo) repo-A  ✕               $(refresh) ⋯ │
├──────────────────────────────────────────────┤
│   Glance     Find      Setup                 │
│                       ──────                 │
├──────────────────────────────────────────────┤
│ Binary                                       │
│ $(check)  session-recall 0.1.0               │
│           %LOCALAPPDATA%\auto-memory         │
│           PATH: user                         │
│           [ Reinstall ]                      │
│                                              │
│ Backends                                     │
│ $(circle-slash) SQLite        not present    │
│ $(check)        Session-state 29 sessions    │
│ $(check)        VS Code Chat  86 sessions    │
│                                              │
│ Copilot instructions                         │
│ $(circle) A Global       not set    [ Add  ] │
│ $(check)  B Workspace    set        [ Remove│
│                                              │ │
│ $(circle) C Settings     not set    [ Add  ] │
│                                              │
│ ▸ Diagnostics                                │
└──────────────────────────────────────────────┘
```

Diagnostics is a collapsed expander; clicking it reveals copy / open-issue
buttons and the report body.

---

## Editor tab — Glance (master / detail)

```
┌────────────────────────────────────────────────────────────────────────┐
│ $(repo) repo-A  ✕                                  $(refresh) ⋯       │
├────────────────────────────────────────────────────────────────────────┤
│   Glance     Find      Setup                                          │
│   ─────────                                                            │
├──────────────────────────────────┬─────────────────────────────────────┤
│ Last session                     │  Repo Coverage              × close │
│ ┌──────────────────────────────┐ │ ┌─────────────────────────────────┐ │
│ │ Apr 26 14:02 chat            │ │ │ Score: 2.0   RED                │ │
│ │ Refactor TranscriptReader…   │ │ │ Threshold: ≥ 5 sessions         │ │
│ │ [Resume]                     │ │ │ Found: 0 sessions for repo-A    │ │
│ └──────────────────────────────┘ │ │                                 │ │
│                                  │ │ What this means:                │ │
│ Health                  8.4/10  │ │ AutoMemory has very little      │ │
│ ████████████████████░░          │ │ context for the open repo.      │ │
│                                  │ │ New Copilot Chat sessions in    │ │
│ $(pass) Freshness    10.0   ›   │ │ this workspace improve it.      │ │
│ $(pass) Corpus       10.0   ›   │ │                                 │ │
│ $(error) Repo Cover   2.0   ●   │ │ [Open Copilot Chat]             │ │
│ $(pass) Summary      10.0   ›   │ │ [Hide for 7 days]               │ │
│ $(pass) Recent       10.0   ›   │ └─────────────────────────────────┘ │
└──────────────────────────────────┴─────────────────────────────────────┘
```

Click a dimension on the left → its detail card replaces whatever is on the
right. The right panel always has *something* in it — by default the
worst-zone dimension's detail.

---

## Editor tab — Find (master / detail)

```
┌────────────────────────────────────────────────────────────────────────┐
│ $(repo) repo-A  ✕                                  $(refresh) ⋯       │
├────────────────────────────────────────────────────────────────────────┤
│   Glance     Find      Setup                                          │
│             ──────                                                     │
├──────────────────────────────────┬─────────────────────────────────────┤
│ [ search sessions…       ] [Go]  │ Session 0a1b2c3d                    │
│ Sources: ◉ SQL ◉ State ◉ Chat    │ Apr 26 14:02 • repo-A • chat        │
│                                  │ Turns: 7 • Tools: 12                │
│ Apr 26 repo-A chat               │ [Resume] [Copy md] [Reveal]         │
│  Refactor TranscriptReader…   ●  │ ──────────────────────────────────  │
│ Apr 26 repo-A state              │ ▾ Summary                           │
│  M8 step 03 implementation       │   Refactored TranscriptReader…      │
│ Apr 25 repo-A chat               │ ▾ Turn 1 — user                     │
│  Add Enter-key search handler    │   Can you switch this to            │
│ …                                │   File.ReadLines for streaming?     │
│                                  │ ▾ Turn 1 — assistant                │
│ 12 of 86 (filtered)              │   Yes — here's the diff…            │
│                                  │ ▸ Turn 2 — user (collapsed)         │
│                                  │ ▸ Turn 2 — assistant (collapsed)    │
│                                  │ …                                   │
└──────────────────────────────────┴─────────────────────────────────────┘
```

The selected row carries a left-edge accent bar (`●` in the wireframe).

---

## Editor tab — Setup

Setup on the editor tab is the same content as the sidebar but in two
columns: install / backends on the left, instructions / diagnostics on the
right. No master/detail behaviour is needed — the form is short.

```
┌────────────────────────────────────────────────────────────────────────┐
│ Binary                          │ Copilot instructions                 │
│ $(check) session-recall 0.1.0   │ $(circle) A Global       [ Add  ]    │
│ %LOCALAPPDATA%\auto-memory      │ $(check)  B Workspace    [ Remove ]  │
│ PATH: user                      │ $(circle) C Settings     [ Add  ]    │
│ [ Reinstall ]                   │                                      │
│                                 │ ▾ Diagnostics                        │
│ Backends                        │ Extension: 0.1.0                     │
│ $(slash) SQLite     missing     │ Binary:    0.1.0                     │
│ $(check) State      29          │ OS:        win32 x64                 │
│ $(check) Chat       86          │ Backends:  29 state, 86 chat         │
│                                 │ Last error: —                        │
│                                 │ [ Copy report ] [ Open issue ]       │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Empty / loading / error state pattern

Every panel uses the same three-shape pattern:

```
┌── Loading ────────────────────────────┐
│  $(loading~spin) Loading sessions…    │
└────────────────────────────────────────┘

┌── Empty ──────────────────────────────┐
│  $(inbox)                              │
│  No sessions yet                       │
│  Start a Copilot Chat to populate      │
│  recall — they appear here as soon as  │
│  Copilot writes them.                  │
│  [ Open Copilot Chat ]                 │
└────────────────────────────────────────┘

┌── Error ──────────────────────────────┐
│  $(error)                              │
│  Couldn't read sessions                │
│  spawn session-recall ENOENT           │
│  [ Retry ]  [ Reinstall binary ]       │
└────────────────────────────────────────┘
```

The codicon, headline, body, and primary action are positional — every
empty/error block fills the same four slots so users learn the shape.
