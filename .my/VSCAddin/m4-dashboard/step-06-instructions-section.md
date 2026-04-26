# Step 06 — Instructions section polish

## Goal
Polish the instructions section: friendly labels, post-action toast, defensive handling for "no workspace".

## Edits to `media/panel.js` `renderInstructions`

```javascript
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
```

## Add error toast handler

```javascript
case 'error':
  showError(payload.message);
  break;
```

```javascript
function showError(msg) {
  const banner = document.createElement('div');
  banner.style.cssText = 'background: var(--vscode-inputValidation-errorBackground); padding: 6px; margin: 8px 0;';
  banner.textContent = msg;
  document.body.prepend(banner);
  setTimeout(() => banner.remove(), 5000);
}
```

## Done when
- [ ] Instructions row shows two-line label (name + path)
- [ ] Add/Remove button updates badge after action without full reload
- [ ] Disabled state shown when no workspace open (for B and C)
- [ ] Errors surface as red banner
