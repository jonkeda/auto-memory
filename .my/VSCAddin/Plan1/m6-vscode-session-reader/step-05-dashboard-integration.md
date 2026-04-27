# Step 05 — Dashboard integration

## Goal

The extension detects which storage format is present and adjusts the dashboard
accordingly — no user configuration required.

## Detection logic (already in `StorageDetect.cs`)

`StorageDetect.VsCodeSessionCount()` returns > 0 when flat-file sessions exist.
The CLI commands now fall through to the flat-file reader automatically.

The extension already passes `source` in JSON responses from Step 04.
The dashboard needs to:

1. Show a **"VS Code sessions"** badge/tag when `source === "vscode-session-state"`
2. Hide the "HEALTH" section (replace with a notice) when in VS Code mode
3. Show the session count in the install-status bar

## Edits to `media/panel.js`

### 1. Source badge in `renderSessions`

```javascript
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
  }

  for (const s of data.sessions ?? []) {
    // ... existing row rendering ...
  }
}
```

### 2. Health section notice in VS Code mode

```javascript
function renderHealth(data) {
  const c = document.querySelector('#health .content');
  c.innerHTML = '';
  if (data.error) { c.textContent = data.error; return; }
  if (data.warning) { renderNoDb(c, data); return; }
  // ... existing rendering ...
}
```

`renderNoDb` already shows the VS Code card when `data.storage_format === 'vscode-session-state'`.
No further changes needed here — health always returns the VS Code detection payload
when SQLite is absent (from `HealthCommand`).

### 3. Install status — show session count

In `renderInstall`:

```javascript
function renderInstall(data) {
  let text = `${data.version ?? 'not installed'} • ${data.installDir}`;
  if (data.vscode_session_count > 0) {
    text += ` • ${data.vscode_session_count} VS Code sessions`;
  }
  document.getElementById('install-status').textContent = text;
}
```

## Edits to `src/panel/messages.ts` — `postInstall`

```typescript
async function postInstall(p: AutoMemoryPanel): Promise<void> {
  const v = await p.binary().installedVersion();
  const vsCodeCount = await p.runCli(['schema-check', '--count-vscode']).catch(() => '0');
  // Note: simpler to just call StorageDetect via the health response; 
  // extract session_state_count from the cached health payload if available.
  p.post('installStatus', {
    version: v,
    installDir: p.binary().installDir(),
    pathStrategy: 'userPath',
    // vscode_session_count populated by the extension host; see BinaryManager
  });
}
```

> **Simpler alternative**: the `health --json` response already contains `session_state_count`
> (set by `StorageDetect`). Cache it in the panel and forward it to `installStatus`.
> This avoids a separate CLI call.

## Done when

- [ ] Sessions list shows "● VS Code Copilot sessions" badge when `source === "vscode-session-state"`
- [ ] Health section shows VS Code onboarding card (not a loading spinner) when in VS Code mode
- [ ] Install-status row shows VS Code session count
- [ ] Clicking a session row sends `showSession` and the extension fetches `show <id> --json`
- [ ] Search works end-to-end: type query → click Go → results from `search --json`
