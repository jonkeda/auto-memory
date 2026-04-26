# Step 03 — HTML template

## Goal
Build the static webview HTML with CSP, theme variables, and section anchors.

## File: `vscode-extension/src/panel/html.ts`

```typescript
import * as vscode from 'vscode';

export function renderHtml(webview: vscode.Webview, extUri: vscode.Uri, surface: 'sidebar'|'tab'): string {
  const nonce = randomNonce();
  const cssUri = webview.asWebviewUri(vscode.Uri.joinPath(extUri, 'media', 'panel.css'));
  const jsUri  = webview.asWebviewUri(vscode.Uri.joinPath(extUri, 'media', 'panel.js'));
  const csp = `default-src 'none'; img-src ${webview.cspSource}; style-src ${webview.cspSource}; script-src 'nonce-${nonce}';`;

  return `<!DOCTYPE html>
<html data-surface="${surface}">
<head>
  <meta http-equiv="Content-Security-Policy" content="${csp}">
  <link rel="stylesheet" href="${cssUri}">
</head>
<body>
  <header id="install-status">…</header>
  <section id="health"><h2>Health</h2><div class="content">Loading…</div></section>
  <section id="sessions"><h2>Recent sessions</h2><div class="content">Loading…</div></section>
  <section id="search">
    <h2>Search</h2>
    <input id="search-input" type="text" placeholder="Search sessions…">
    <button id="search-btn">Go</button>
    <div id="search-results"></div>
  </section>
  <section id="instructions"><h2>Copilot instructions</h2><div class="content">Loading…</div></section>
  <button id="refresh">Refresh</button>
  <script nonce="${nonce}" src="${jsUri}"></script>
</body>
</html>`;
}

function randomNonce(): string {
  return [...crypto.getRandomValues(new Uint8Array(16))].map(b => b.toString(16).padStart(2,'0')).join('');
}
```

## File: `vscode-extension/media/panel.css`

```css
body { font-family: var(--vscode-font-family); color: var(--vscode-foreground); background: var(--vscode-editor-background); padding: 12px; }
h2 { font-size: 13px; text-transform: uppercase; opacity: 0.7; margin: 16px 0 8px; }
section { border-top: 1px solid var(--vscode-panel-border); padding-top: 8px; }
button { background: var(--vscode-button-background); color: var(--vscode-button-foreground); border: 0; padding: 4px 10px; cursor: pointer; }
button:hover { background: var(--vscode-button-hoverBackground); }
input { background: var(--vscode-input-background); color: var(--vscode-input-foreground); border: 1px solid var(--vscode-input-border); padding: 4px; width: 100%; box-sizing: border-box; }
.row { display: flex; align-items: center; justify-content: space-between; padding: 4px 0; }
.badge-ok { color: var(--vscode-testing-iconPassed); }
.badge-warn { color: var(--vscode-testing-iconQueued); }
.score-bar { height: 4px; background: var(--vscode-progressBar-background); border-radius: 2px; }
[data-surface="sidebar"] #search-results .full-detail { display: none; }
```

## File: `vscode-extension/media/panel.js`

```javascript
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
function renderHealth(data) { document.querySelector('#health .content').textContent = JSON.stringify(data, null, 2); }
function renderSessions(data) { document.querySelector('#sessions .content').textContent = JSON.stringify(data, null, 2); }
function renderSearch(data) { document.getElementById('search-results').textContent = JSON.stringify(data, null, 2); }
function renderInstall(data) { document.getElementById('install-status').textContent = `${data.version ?? 'not installed'} • ${data.installDir} • PATH: ${data.pathStrategy}`; }
function renderInstructions(data) {
  const c = document.querySelector('#instructions .content');
  c.innerHTML = '';
  for (const key of ['A','B','C']) {
    const s = data[key];
    const row = document.createElement('div');
    row.className = 'row';
    const left = document.createElement('span');
    left.textContent = `${key}  ${s.installed ? '✅' : '⚠️'}  ${s.path}`;
    left.className = s.installed ? 'badge-ok' : 'badge-warn';
    const btn = document.createElement('button');
    btn.textContent = s.installed ? 'Remove' : 'Add';
    btn.addEventListener('click', () => vscode.postMessage({
      type: s.installed ? 'removeInstructions' : 'addInstructions',
      payload: { strategy: key }
    }));
    row.append(left, btn);
    c.appendChild(row);
  }
}

requestAll();
```

Add `media/` to `.vscodeignore` exclusion list (i.e. INCLUDE it in package).

## Done when
- [ ] CSP allows webview to load `panel.css` and `panel.js`
- [ ] Theme colours follow active VS Code theme
- [ ] No CSP violations in webview devtools
