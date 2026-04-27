# M8 — Health reads VS Code sessions

## Goal

When no SQLite `session-store.db` is found, the `health --json` command currently returns
a stub `{"overall_score":null,"dimensions":[]}` with metadata only.  

This milestone makes Health **compute real scores** from the two available flat-file
backends (session-state + VS Code Chat transcripts) so the dashboard shows meaningful
information instead of the "no database found" card.

## Current state (screenshot)

> **HEALTH — VS Code Copilot sessions detected (44 sessions)**  
> auto-memory currently reads the Copilot CLI database (session-store.db).  
> VS Code Copilot stores sessions as flat files in ~/.copilot/session-state/  
> — support for this format is planned.  
> [Reload]

After M8 the Health panel shows a real score with flat-file-appropriate dimensions.

## Steps

| # | File | Title |
|---|---|---|
| 01 | `step-01-flat-health-context.md` | `FlatHealthContext`: aggregate stats from both flat-file stores |
| 02 | `step-02-flat-dimensions.md`     | 5 simplified health dimensions for flat-file mode |
| 03 | `step-03-health-command.md`      | `HealthCommand` falls through to flat-file health when no SQLite |
| 04 | `step-04-dashboard.md`           | Panel renders flat-file health scores (reuse existing zone cards) |
| 05 | `step-05-tests.md`               | Tests with fixture data |

## Flat-file health dimensions

The 9 SQLite dimensions become 5 simplified flat-file equivalents:

| Dimension | SQLite source | Flat-file equivalent |
|---|---|---|
| **Freshness** | DB modified time | newest session `updated_at` |
| **Corpus Size** | session count | total sessions (state + chat) |
| **Repo Coverage** | sessions for current repo | sessions matching cwd repo |
| **Summary Coverage** | % sessions with summary | % sessions with non-empty summary |
| **Recent Activity** | telemetry turn rate | sessions updated in last 7 days |

Skipped (not applicable): Schema Integrity, Query Latency, Concurrency, E2E Probe,
Progressive Disclosure.

## JSON output shape

```json
{
  "overall_score": 7.2,
  "dims": [
    { "name": "Freshness",         "score": 9.0, "zone": "GREEN",  "detail": "2h ago",           "hint": "" },
    { "name": "Corpus Size",       "score": 7.5, "zone": "GREEN",  "detail": "130 sessions",      "hint": "" },
    { "name": "Repo Coverage",     "score": 5.0, "zone": "AMBER",  "detail": "3 for jonkeda/auto-memory", "hint": "" },
    { "name": "Summary Coverage",  "score": 8.0, "zone": "GREEN",  "detail": "82% (107/130)",     "hint": "" },
    { "name": "Recent Activity",   "score": 9.5, "zone": "GREEN",  "detail": "12 sessions (7d)",  "hint": "" }
  ],
  "top_hints": [],
  "source": "vscode-flat",
  "backends": { "session_state": 44, "vscode_chat": 86 }
}
```

## Panel changes

- `renderHealth` already renders `dims` array — works as-is.
- Add `source === 'vscode-flat'` badge: `● Flat-file mode (no SQLite DB)`.
- Remove the `renderNoDb` call when health has `overall_score !== null`.

## Coexistence

- SQLite path unchanged — if DB exists, all 9 dimensions run as before.
- Flat-file path activates only on `DatabaseNotFoundException`.
- Adding SQLite later (Copilot CLI installed) auto-switches back to full mode.
