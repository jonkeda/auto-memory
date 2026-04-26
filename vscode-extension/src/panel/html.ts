import * as vscode from 'vscode';
import { randomBytes } from 'node:crypto';

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
  return randomBytes(16).toString('hex');
}
