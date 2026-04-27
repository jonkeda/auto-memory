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
  
  // Check for overall_score before warning
  if (data.overall_score !== null && data.overall_score !== undefined) {
    // Flat-file mode banner
    if (data.source === 'vscode-flat') {
      const banner = document.createElement('div');
      banner.className = 'banner info';
      const ss = data.backends?.session_state ?? 0;
      const ch = data.backends?.vscode_chat ?? 0;
      banner.innerHTML = `<strong>Flat-file mode</strong> — ${ss} Copilot CLI sessions + ${ch} VS Code Chat sessions<br><em>Install Copilot CLI for full SQLite-backed health scores.</em>`;
      c.appendChild(banner);
    }
    
    // Overall score
    const overall = document.createElement('div');
    overall.style.fontSize = '24px';
    overall.style.color = scoreColour(data.overall_score);
    overall.textContent = `${data.overall_score?.toFixed(1) ?? '?'} / 10`;
    c.appendChild(overall);
    
    // Dimensions - normalize dims (flat-file) vs dimensions (SQLite)
    const dimensions = data.dims ?? data.dimensions ?? [];
    for (const dim of dimensions) {
      const row = document.createElement('div');
      row.className = 'row';
      const label = document.createElement('span');
      label.textContent = `${zoneIcon(dim.zone)} ${dim.name}`;
      const score = document.createElement('span');
      score.textContent = `${dim.score?.toFixed(1) ?? '-'}`;
      row.append(label, score);
      c.appendChild(row);
    }
    
    // Top hints
    if (data.top_hints?.length) {
      const hintsList = document.createElement('ul');
      hintsList.className = 'hints';
      hintsList.style.cssText = 'margin-top:8px; padding-left:20px; font-size:12px; opacity:0.8;';
      for (const hint of data.top_hints) {
        const li = document.createElement('li');
        li.textContent = hint;
        hintsList.appendChild(li);
      }
      c.appendChild(hintsList);
    }
  } else if (data.warning) {
    renderNoDb(c, data);
  } else {
    // Fallback - shouldn't normally reach here
    c.textContent = 'No health data available';
  }
}

function renderSessions(data) {
  const c = document.querySelector('#sessions .content');
  c.innerHTML = '';
  if (data.error) { c.textContent = data.error; return; }
  if (data.warning && !data.sessions?.length) { renderNoDb(c, data); return; }

  // Show source badge if coming from flat-file reader
  if (data.source === 'vscode-session-state') {
    const badge = document.createElement('div');
    badge.style.cssText = 'font-size:10px; opacity:0.6; margin-bottom:6px;';
    badge.textContent = '● VS Code Copilot sessions';
    c.appendChild(badge);
  } else if (data.source === 'vscode-chat') {
    const badge = document.createElement('div');
    badge.style.cssText = 'font-size:10px; opacity:0.6; margin-bottom:6px;';
    badge.textContent = '● VS Code Chat sessions';
    c.appendChild(badge);
  }

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
  let text = `${data.version ?? 'not installed'} • ${data.installDir} • PATH: ${data.pathStrategy}`;
  if (data.vscode_session_count > 0) {
    text += ` • ${data.vscode_session_count} Copilot CLI sessions`;
  }
  if (data.vscode_chat_count > 0) {
    text += ` • ${data.vscode_chat_count} VS Code Chat sessions`;
  }
  document.getElementById('install-status').textContent = text;
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

function renderNoDb(container, data) {
  const wrap = document.createElement('div');
  wrap.style.cssText = 'font-size:12px; line-height:1.6;';
  const msg = document.createElement('div');

  const stateCount = data.session_state_count ?? 0;
  const chatCount = data.vscode_chat_count ?? 0;
  const total = stateCount + chatCount;

  if (data.storage_format === 'vscode-session-state' || chatCount > 0) {
    msg.innerHTML =
      `<strong>VS Code Copilot sessions detected (${total} sessions)</strong><br>` +
      `<strong>${stateCount}</strong> Copilot CLI sessions in <code>~/.copilot/session-state/</code><br>` +
      `<strong>${chatCount}</strong> Copilot Chat sessions in <code>workspaceStorage/</code><br>` +
      `Both are readable. Use <strong>Reload</strong> to refresh.`;
  } else {
    msg.textContent = 'No session database found. The GitHub Copilot CLI creates it automatically when you run your first agentic session.';
    const path = document.createElement('div');
    path.style.cssText = 'margin-top:4px; font-family:monospace; font-size:11px; word-break:break-all; opacity:0.7;';
    path.textContent = `Expected: ${data.db_path ?? '~/.copilot/session-store.db'}`;
    wrap.append(msg, path);
  }

  const btn = document.createElement('button');
  btn.textContent = 'Reload';
  btn.style.marginTop = '8px';
  btn.addEventListener('click', () => requestAll());
  if (data.storage_format === 'vscode-session-state' || chatCount > 0) {
    wrap.append(msg, btn);
  } else {
    wrap.append(btn);
  }
  container.appendChild(wrap);
}

function showError(msg) {
  const banner = document.createElement('div');
  banner.style.cssText = 'background: var(--vscode-inputValidation-errorBackground); padding: 6px; margin: 8px 0;';
  banner.textContent = msg;
  document.body.prepend(banner);
  setTimeout(() => banner.remove(), 5000);
}

requestAll();
