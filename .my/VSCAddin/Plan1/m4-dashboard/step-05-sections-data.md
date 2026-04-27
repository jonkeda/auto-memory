# Step 05 — Section renderers (data-rich)

## Goal
Replace placeholder JSON-dump rendering in `panel.js` with real renderers for health, sessions, search.

## Edits to `media/panel.js`

```javascript
function renderHealth(data) {
  const c = document.querySelector('#health .content');
  c.innerHTML = '';
  const overall = document.createElement('div');
  overall.style.fontSize = '24px';
  overall.style.color = scoreColour(data.overall_score);
  overall.textContent = `${data.overall_score?.toFixed(1) ?? '?'} / 10`;
  c.appendChild(overall);
  for (const dim of data.dimensions ?? []) {
    const row = document.createElement('div');
    row.className = 'row';
    const label = document.createElement('span');
    label.textContent = `${zoneIcon(dim.zone)} ${dim.name}`;
    const score = document.createElement('span');
    score.textContent = `${dim.score?.toFixed(1) ?? '-'}`;
    row.append(label, score);
    c.appendChild(row);
  }
}

function renderSessions(data) {
  const c = document.querySelector('#sessions .content');
  c.innerHTML = '';
  for (const s of data.sessions ?? []) {
    const row = document.createElement('div');
    row.className = 'row session-row';
    row.style.cursor = 'pointer';
    row.textContent = `${s.date}  ${s.repo ?? '-'}  ${s.summary ?? ''}`;
    row.addEventListener('click', () =>
      vscode.postMessage({ type: 'showSession', payload: { id: s.id } }));
    c.appendChild(row);
  }
}

function renderSearch(data) {
  const c = document.getElementById('search-results');
  c.innerHTML = '';
  if (data.warning) { c.textContent = data.warning; return; }
  for (const r of data.results ?? []) {
    const card = document.createElement('div');
    card.className = 'row';
    card.textContent = `[${r.source}] ${r.excerpt}  (${r.date})`;
    c.appendChild(card);
  }
}

function zoneIcon(zone) {
  return zone === 'green' ? '🟢' : zone === 'amber' ? '🟡' : '🔴';
}
function scoreColour(s) {
  if (s == null) return 'inherit';
  if (s >= 7) return 'var(--vscode-testing-iconPassed)';
  if (s >= 4) return 'var(--vscode-testing-iconQueued)';
  return 'var(--vscode-testing-iconFailed)';
}
```

## Done when
- [ ] Health renders dimensions with zone icons + overall score
- [ ] Sessions list rows are clickable, send `showSession`
- [ ] Search shows warning text when binary returns warning, results otherwise
- [ ] No raw JSON visible to user
