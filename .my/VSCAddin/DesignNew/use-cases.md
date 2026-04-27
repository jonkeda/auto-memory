# Use cases

Twelve user goals, ordered by frequency × value. Each has a trigger,
expected steps, and a success criterion. The IA in
[information-architecture.md](information-architecture.md) and wireframes in
[wireframes.md](wireframes.md) are derived from this list.

Personas are kept light — most users wear several hats:

- **Daily Dev** — uses Copilot Chat / Copilot CLI in their normal flow,
  rarely thinks about AutoMemory until something breaks.
- **Tinkerer** — installed AutoMemory, wants to verify it works.
- **Reviewer** — looking for context to put into a PR or commit message.
- **Multi-repo Dev** — works across several repos in one VS Code window.

---

## UC-1 — Verify install (Tinkerer)

**Trigger:** Just installed the VSIX; opens the Activity Bar icon.

**Steps:**
1. Click AutoMemory icon in Activity Bar.
2. Sidebar opens on the **Setup** tab (auto-selected because health and
   instructions are not yet green).
3. Status row shows `session-recall 0.1.0 ✓ on PATH`.
4. Below: three instructions strategies with status; a green `Add B
   (workspace)` is highlighted as the recommended action.

**Success:** User sees a clear "you're set up" or "do this next" within
3 seconds of opening the panel.

---

## UC-2 — Daily glance (Daily Dev)

**Trigger:** User clicks the AutoMemory status-bar item to confirm everything
is healthy.

**Steps:**
1. A small popover (or the sidebar **Glance** tab) shows: overall score,
   one-line "X recent sessions" stat, and a green/amber/red dot for each
   dimension.
2. If everything is green, no further action.

**Success:** Two-second visual confirmation that recall is healthy. No
scrolling, no clicks.

---

## UC-3 — Find a past conversation (Daily Dev / Reviewer)

**Trigger:** User remembers asking about *something with React hooks* a few
days ago and wants to find it.

**Steps:**
1. Open AutoMemory sidebar → **Find** tab (or `Ctrl+Shift+P → AutoMemory:
   Search`).
2. Cursor is auto-focused in the search input.
3. Type "react hooks", press Enter.
4. Results list shows date, repo, source, and a 1-line excerpt; matched
   terms are highlighted.
5. Click a result → opens the session detail view (UC-5).

**Success:** From keystroke to opened session detail in under 10 seconds.

---

## UC-4 — Resume the most recent session (Daily Dev)

**Trigger:** User just reopened VS Code and wants to pick up where they left
off.

**Steps:**
1. Sidebar → **Glance** tab.
2. The "Last session" card at the top shows date, repo, summary, and a
   `Resume` button.
3. Click `Resume` → opens session detail in a new editor tab.

**Success:** One click from panel-open to full transcript.

---

## UC-5 — Open a full session transcript (Reviewer)

**Trigger:** Selected a session (from Find / Glance) and wants to read the
whole thing.

**Steps:**
1. Click the session row.
2. A new editor tab opens with the AutoMemory **Session Detail** view.
3. Header: date, repo, source chip, turn / tool counts.
4. Body: rendered turns, collapsed by default with `Expand all` button.
5. Toolbar: `Copy summary`, `Copy as markdown`, `Reveal source file`.

**Success:** Reads the full conversation without leaving VS Code; can copy
content into a PR.

---

## UC-6 — Copy a session summary into a PR (Reviewer)

**Trigger:** Wrapping up a feature; wants to paste the AI-generated summary
into the PR description.

**Steps:**
1. Find the session (UC-3) or pick from Glance recent list (UC-4).
2. On the session row, click the kebab `⋯` → `Copy summary`.
3. Toast: `Summary copied`.

**Success:** Two clicks from panel to clipboard; no context switch to a
terminal.

---

## UC-7 — Drill into a low health score (Daily Dev)

**Trigger:** Glance shows `Repo Coverage` is RED.

**Steps:**
1. Click the red dimension row.
2. The card expands inline (sidebar) or opens a side-by-side detail panel
   (tab) with:
   - Plain-language explanation of what the dimension measures.
   - The current value vs. the threshold.
   - Suggested next action (e.g. "Run a Copilot session in this repo to
     populate recall context") with an `Open Copilot Chat` button.

**Success:** User understands what a red dimension means and what to do
next, without reading external docs.

---

## UC-8 — Filter to current repo (Multi-repo Dev)

**Trigger:** User opens VS Code on `repo-A`; wants only sessions related to
that repo.

**Steps:**
1. Sidebar opens. A repo chip near the top reads `Repo: repo-A ✕`.
2. Sessions list, search, and Glance counts are all pre-filtered to
   repo-A.
3. Click `✕` on the chip to clear, or click the chip itself to switch to
   "All repos".

**Success:** Repo filter is obvious, removable, and applies to every list
in the panel.

---

## UC-9 — Audit instructions setup in this workspace (Tinkerer / Reviewer)

**Trigger:** Joined a new repo; wants to know whether AutoMemory's recall
instructions are wired into Copilot for this workspace.

**Steps:**
1. Sidebar → **Setup** tab.
2. Three rows for strategies A (global), B (workspace), C (settings) with
   green/grey status circles and `Add` / `Remove` buttons.
3. Hovering a row shows the path it would write to.

**Success:** User can answer "is recall on for this repo?" and toggle it
in under 5 seconds.

---

## UC-10 — Refresh after Copilot writes a new session (Daily Dev)

**Trigger:** User just finished a Copilot Chat conversation; expects it to
appear in the panel without explicit refresh.

**Steps:**
1. Host watches `~/.copilot/session-state/` and the relevant
   `workspaceStorage/.../GitHub.copilot-chat/transcripts/` directories.
2. On a new file, host invalidates cache and posts updated payloads.
3. Panel updates the Glance "Last session" card and the Sessions list with
   a brief highlight pulse on the new row.

**Success:** New sessions appear within 5 seconds of being written, without
a manual click.

---

## UC-11 — Diagnose a bug / file an issue (Tinkerer)

**Trigger:** Something looks wrong; user wants to capture state for a bug
report.

**Steps:**
1. Setup tab → `Diagnostics` button.
2. Opens a generated markdown report with: extension version, binary
   version, OS, install path, PATH strategy, backend counts, and the last
   error message if any.
3. Buttons: `Copy report`, `Open issue`.

**Success:** One-click reproducible diagnostic; no manual data gathering.

---

## UC-12 — Toggle source visibility (Daily Dev)

**Trigger:** User trusts SQLite sessions but wants to ignore noisier VS
Code Chat transcripts.

**Steps:**
1. Sessions list header has source chips: `[ SQLite ✓ ] [ Session-state ✓ ]
   [ VS Code Chat ✓ ]`.
2. Click a chip to toggle that source off; the list and counts update.
3. Preference persists in the workspace state.

**Success:** User narrows the dataset without leaving the panel; setting
sticks across reloads.

---

## Priority

Tier 1 (must support in the first redesign milestone): UC-1, UC-2, UC-3,
UC-4, UC-9.

Tier 2 (next): UC-5, UC-7, UC-8.

Tier 3 (later): UC-6, UC-10, UC-11, UC-12.
