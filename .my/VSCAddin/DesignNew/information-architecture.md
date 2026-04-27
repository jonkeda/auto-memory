# Information architecture

The redesigned panel uses a **3-tab header** instead of one long scroll. The
tabs are picked to match the dominant verbs across [use-cases.md](use-cases.md):

| Tab | Verb | Use cases |
|---|---|---|
| **Glance** | "Is everything OK?" | UC-2, UC-4, UC-7, UC-10 |
| **Find** | "Where did I…?" | UC-3, UC-5, UC-6, UC-8, UC-12 |
| **Setup** | "Configure / verify" | UC-1, UC-9, UC-11 |

```
┌──────────────────────────────────────────────┐
│  Repo: repo-A  ✕                  [⟳]  [⋯]   │  context bar
├──────────────────────────────────────────────┤
│  ◉ Glance    ○ Find    ○ Setup               │  tab bar
├──────────────────────────────────────────────┤
│                                              │
│            (active tab content)              │
│                                              │
└──────────────────────────────────────────────┘
```

The context bar (repo chip + refresh + overflow menu) is global — visible
on every tab. The overflow menu hosts low-frequency actions (open
diagnostics, change settings, open detail in a new tab).

## Two surfaces, one IA

| Surface | Width | Tabs work as | Detail views |
|---|---|---|---|
| **Sidebar view** | ~280–360 px | Three-button segment | Inline expand on Glance / Find; "Open in tab" link for full transcript |
| **Editor tab** | ≥ 800 px | Same segment, same routes | Side-by-side master/detail (list left, detail right) |

The same code paths and message protocol drive both. Surface adapts via the
existing `data-surface="sidebar" | "tab"` attribute.

## Glance tab content

```
┌──────────────────────────────────────────────┐
│  ◉ Glance    ○ Find    ○ Setup               │
├──────────────────────────────────────────────┤
│  ┌─ Last session ─────────────────────┐      │
│  │  2026-04-26 14:02 • repo-A         │      │
│  │  "Refactor TranscriptReader to use │      │
│  │   File.ReadLines"                  │      │
│  │  [ Resume ]  [ Copy summary ]      │      │
│  └────────────────────────────────────┘      │
│                                              │
│  Health                                      │
│  ┌─────────────────── 8.4 / 10 ─┐             │
│  │ ●●●●●●●●●○                  │             │
│  └────────────────────────────────┘          │
│  ● Freshness            10.0   ›             │
│  ● Corpus Size          10.0   ›             │
│  ● Repo Coverage         2.0   › red          │
│  ● Summary Coverage     10.0   ›             │
│  ● Recent Activity      10.0   ›             │
│                                              │
│  ↳ Repo Coverage needs attention             │
│    [ Open Copilot Chat in repo-A ]           │
└──────────────────────────────────────────────┘
```

Notes:

- **Last session card** answers UC-4 in zero clicks.
- The big bar at the top of the dimensions list is a quick comparator; the
  individual rows are clickable to expand (UC-7).
- Only the *worst* dimension's hint card is shown by default; the rest are
  reachable via the row's expand chevron.

## Find tab content

```
┌──────────────────────────────────────────────┐
│  ○ Glance    ◉ Find    ○ Setup               │
├──────────────────────────────────────────────┤
│  [🔎  search sessions…              ] [Go]    │
│  Sources:  [SQLite ✓] [State ✓] [Chat ✓]      │
│  Sort:     [Recent ▾]                         │
│                                              │
│  Apr 26  repo-A   chat                       │
│    Refactor TranscriptReader to use…         │
│    [Open] [Copy summary] [⋯]                 │
│  Apr 26  repo-A   state                      │
│    M8 step 03 implementation                 │
│    [Open] [Copy summary] [⋯]                 │
│  …                                           │
│                                              │
│  Showing 12 of 86 (filtered to repo-A)       │
└──────────────────────────────────────────────┘
```

Notes:

- Search input auto-focuses when the tab opens.
- Default state (empty query) shows the most recent N sessions for the
  current filter — Find doubles as the recent-sessions list, removing the
  redundant "Recent sessions" section from the original design.
- Source chips toggle visibility (UC-12). They are CTRL-clickable to
  isolate one source.
- The kebab `⋯` menu hosts low-frequency actions: copy id, copy raw JSON,
  reveal source file, delete from cache.

## Setup tab content

```
┌──────────────────────────────────────────────┐
│  ○ Glance    ○ Find    ◉ Setup               │
├──────────────────────────────────────────────┤
│  Binary                                      │
│  ● session-recall 0.1.0                      │
│    %LOCALAPPDATA%\auto-memory                │
│    PATH: User PATH ✓                         │
│    [ Reinstall ] [ Change install dir ]      │
│                                              │
│  Backends                                    │
│  ● SQLite          missing                   │
│  ● Session-state    29 sessions ✓            │
│  ● VS Code Chat     86 sessions ✓            │
│                                              │
│  Copilot instructions                        │
│  ● A Global         not installed   [ Add ]  │
│  ● B Workspace      installed       [ Remove │
│                                                ]│
│  ● C Settings       not installed   [ Add ]  │
│                                              │
│  ▸ Diagnostics                               │
└──────────────────────────────────────────────┘
```

Notes:

- Backends are first-class status here, not just a footnote on the Glance
  banner.
- The diagnostics row is collapsible to keep the tab scannable; expanding
  it reveals the report and copy/issue buttons (UC-11).

## Detail views (sidebar: inline; tab: side-by-side)

### Health dimension detail

```
┌─ Repo Coverage ─────────────────────── × ─┐
│ Score: 2.0  RED                            │
│ Threshold: ≥ 5 sessions for this repo      │
│ Found: 0 sessions for jonkeda/auto-memory  │
│                                            │
│ What this means:                           │
│ AutoMemory has very little context for     │
│ the currently open repo. New Copilot Chat  │
│ sessions in this workspace will improve    │
│ this score.                                │
│                                            │
│ [ Open Copilot Chat ] [ Hide for 7 days ]  │
└────────────────────────────────────────────┘
```

### Session detail (editor tab only)

```
┌─ Session 0a1b2c3d ─ Apr 26 14:02 ── × ──┐
│ Repo: repo-A • Source: chat              │
│ Turns: 7 • Tools: 12                     │
│ [Resume] [Copy summary] [Copy markdown]  │
│ [Reveal file]                            │
├──────────────────────────────────────────┤
│ ▾ Summary                                │
│   Refactored TranscriptReader to stream… │
│ ▾ Turn 1 — user                          │
│   Can you switch this to File.ReadLines? │
│ ▸ Turn 1 — assistant (collapsed)         │
│ ▸ Turn 2 — user (collapsed)              │
│ …                                        │
└──────────────────────────────────────────┘
```

## What disappears

| Old | Replacement |
|---|---|
| Header strip "session-recall 0.1.0 • path • PATH" | Moved into Setup tab; install state is no longer the first thing the user sees |
| Standalone "Recent sessions" section | Default state of Find tab |
| Standalone "Search" section | Find tab |
| Standalone "Health" + "Instructions" stacked | Glance + Setup tabs |
| Inline `Refresh` button at the bottom | `⟳` icon in the global context bar |
| `renderNoDb` block | Empty/error states are tab-local; SQLite-missing is no longer "no database" — it's just "SQLite source unavailable" inside the Setup → Backends list, while Glance and Find still work over the flat backends |
