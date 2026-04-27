# RCA-002 — "no database found" shown with no actionable guidance

**Date:** 2026-04-26  
**Severity:** P2 — extension appears broken on fresh install  
**Status:** Fixed

---

## Symptom

After installing the VSIX the dashboard shows:

```
HEALTH
no database found

RECENT SESSIONS
no database found
```

The user has no idea what `session-store.db` is, where it should be, or what action to take. The extension looks broken.

---

## Root Cause

The empty-state message (`"no database found"`) was added in RCA-001 as a one-line string. It is technically accurate but provides zero onboarding context:

- No mention of **what** the database is or **who creates it**
- No mention of **where** the file is expected (`~/.copilot/session-store.db`)
- No actionable next step (use the GitHub Copilot CLI; then reload)

A new user who has never run the Copilot CLI will see this message on every page load with no path forward.

### Why the DB doesn't exist

`session-store.db` is created and populated exclusively by the **GitHub Copilot CLI** (`gh copilot` / `copilot` command). The auto-memory extension is read-only — it queries but never writes the database. Until the user runs at least one Copilot CLI session, the file does not exist.

---

## Fix

Replace the bare `"no database found"` text in `panel.js` with a structured onboarding card that:

1. States the expected DB path (`~/.copilot/session-store.db`)
2. Explains that the GitHub Copilot CLI creates it automatically
3. Adds a "Reload" button so the user can refresh after starting a session

---

## Verification

After the fix:

- Fresh install with no `~/.copilot/session-store.db` → dashboard shows onboarding card with path and reload button
- After the Copilot CLI creates the DB and user clicks Reload → sections populate normally
- Existing install with a valid DB → no change to normal rendering path
