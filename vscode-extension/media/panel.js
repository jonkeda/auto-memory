const vscode = acquireVsCodeApi();

window.addEventListener('message', (event) => {
  const { type, payload } = event.data;
  switch (type) {
    case 'health':              renderHealth(payload); break;
    case 'sessions':            renderSessions(payload); break;
    case 'searchResult':        renderSearch(payload); break;
    case 'installStatus':       renderInstall(payload); break;
    case 'instructionsStatus':  renderInstructions(payload); break;
    case 'refreshRequested':    requestAll(); break;
    case 'error':               showError(payload.message); break;
  }
});

document.getElementById('refresh').addEventListener('click', () => {
  vscode.postMessage({ type: 'refresh', payload: { section: 'all' } });
});
document.getElementById('search-btn').addEventListener('click', () => {
  const q = document.getElementById('search-input').value;
  vscode.postMessage({ type: 'search', payload: { query: q } });
});

function requestAll() {
  vscode.postMessage({ type: 'refresh', payload: { section: 'all' } });
}

function renderHealth(data) {
  const c = document.querySelector('#health .content');
  c.innerHTML = '';
  if (data.error) { c.textContent = data.error; return; }
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
  if (data.error) { c.textContent = data.error; return; }
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

function renderInstall(data) {
  document.getElementById('install-status').textContent = `${data.version ?? 'not installed'} • ${data.installDir} • PATH: ${data.pathStrategy}`;
}

const LABELS = {
  A: { name: 'Global',   sub: '~/.copilot/copilot-instructions.md' },
  B: { name: 'Scoped',   sub: '.github/instructions/session-recall.instructions.md' },
  C: { name: 'Settings', sub: 'github.copilot.chat.codeGeneration.instructions' },
};

function renderInstructions(data) {
  const c = document.querySelector('#instructions .content');
  c.innerHTML = '';
  for (const key of ['A','B','C']) {
    const s = data[key];
    const meta = LABELS[key];
    const row = document.createElement('div');
    row.className = 'row';
    const left = document.createElement('div');
    const title = document.createElement('div');
    title.textContent = `${key} — ${meta.name}`;
    title.className = s.installed ? 'badge-ok' : 'badge-warn';
    const sub = document.createElement('div');
    sub.style.opacity = '0.6';
    sub.style.fontSize = '11px';
    sub.textContent = `${s.installed ? '✅ Installed' : '⚠️ Not installed'} — ${meta.sub}`;
    left.append(title, sub);
    const btn = document.createElement('button');
    btn.textContent = s.installed ? 'Remove' : 'Add';
    const noWorkspace = (key !== 'A') && s.path?.includes('(no workspace)');
    if (noWorkspace) { btn.disabled = true; btn.title = 'Open a workspace folder first'; }
    btn.addEventListener('click', () => vscode.postMessage({
      type: s.installed ? 'removeInstructions' : 'addInstructions',
      payload: { strategy: key }
    }));
    row.append(left, btn);
    c.appendChild(row);
  }
}

function showError(msg) {
  const banner = document.createElement('div');
  banner.style.cssText = 'background: var(--vscode-inputValidation-errorBackground); padding: 6px; margin: 8px 0;';
  banner.textContent = msg;
  document.body.prepend(banner);
  setTimeout(() => banner.remove(), 5000);
}

requestAll();
