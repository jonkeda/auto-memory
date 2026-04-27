# Step 04 — Dashboard: render flat-file health scores

## Goal

Update `panel.js` so the HEALTH card renders properly when `source === 'vscode-flat'`
instead of showing the "no database found / sessions detected" card.

## Current flow

```
panel receives 'health' message
  ├─ data.error → renderError()
  ├─ data.warning (no DB) → renderNoDb()     ← current flat-file path
  └─ data.overall_score present → renderHealth()
```

## New flow (after M8)

```
panel receives 'health' message
  ├─ data.error → renderError()
  ├─ data.overall_score present → renderHealth()   ← flat-file now has scores
  └─ data.warning && !data.overall_score → renderNoDb()  ← only if still no scores
```

## Changes to `panel.js`

### 1. Message handler order fix

```js
function onHealthMessage(data) {
  if (data.error) {
    renderError(data.error);
  } else if (data.overall_score !== null && data.overall_score !== undefined) {
    renderHealth(data);  // move before warning check
  } else if (data.warning) {
    renderNoDb(data);
  } else {
    renderHealth(data);
  }
}
```

### 2. `renderHealth` — flat-file banner

```js
function renderHealth(data) {
  const isFlat = data.source === 'vscode-flat';
  let html = '';

  if (isFlat) {
    const ss = data.backends?.session_state ?? 0;
    const ch = data.backends?.vscode_chat   ?? 0;
    html += `<div class="banner info">
      <strong>Flat-file mode</strong> — ${ss} Copilot CLI sessions + ${ch} VS Code Chat sessions
      <br><em>Install Copilot CLI for full SQLite-backed health scores.</em>
    </div>`;
  }

  // score card header
  const score = data.overall_score !== null ? data.overall_score.toFixed(1) : '—';
  html += `<div class="score-header">Overall: <strong>${score}</strong></div>`;

  // dimension rows (same rendering as before)
  for (const dim of (data.dims ?? data.dimensions ?? [])) {
    html += renderDimRow(dim);
  }

  // top hints
  if (data.top_hints?.length) {
    html += '<ul class="hints">' + data.top_hints.map(h => `<li>${h}</li>`).join('') + '</ul>';
  }

  document.getElementById('health-section').innerHTML = html;
}
```

### 3. `renderDimRow` — handle `dims` vs `dimensions` key name

The CLI emits `"dims"` for flat-file and `"dimensions"` for SQLite.
Normalize in the rendering:

```js
const dimArray = data.dims ?? data.dimensions ?? [];
```

### 4. `renderNoDb` — only shown when `overall_score` is missing

Move the `renderNoDb` call to after the `overall_score` check (already handled in step-03).

## Changes to `panel.css`

Add a `.banner.info` style:

```css
.banner.info {
  background: #e8f4fd;
  border-left: 4px solid #2196f3;
  padding: 8px 12px;
  margin-bottom: 12px;
  border-radius: 4px;
  font-size: 0.9em;
}
```

## Tests

Manual verification (reinstall VSIX, open dashboard):

1. No SQLite DB → HEALTH shows real scores + "Flat-file mode" banner
2. SQLite DB present → HEALTH shows 9 dimensions, no banner
3. Both → SQLite wins (health scores from 9 dims, no flat-file banner)
