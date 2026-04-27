# Theme tokens

The panel uses VS Code's CSS theme variables exclusively for color and
typography, so it inherits Light / Dark / High Contrast themes automatically
without per-theme stylesheets.

## Variables in use

| Variable | Where | Purpose |
|---|---|---|
| `--vscode-font-family` | `body` | Match editor font |
| `--vscode-foreground` | `body` | Default text color |
| `--vscode-editor-background` | `body` | Panel background |
| `--vscode-panel-border` | `section` top border | Section separator |
| `--vscode-button-background` / `…-foreground` / `…-hoverBackground` | `button` | Standard buttons |
| `--vscode-input-background` / `…-foreground` / `…-border` | `input` | Search input |
| `--vscode-progressBar-background` | `.score-bar` | (reserved — currently unused) |
| `--vscode-testing-iconPassed` | `.badge-ok`, `scoreColour() >= 7` | Green status |
| `--vscode-testing-iconQueued` | `.badge-warn`, `scoreColour() >= 4` | Amber status |
| `--vscode-testing-iconFailed` | `scoreColour() < 4` | Red status |
| `--vscode-editorInfo-background` / `…-foreground` / `…-border` | `.banner.info` | Info banner |
| `--vscode-inputValidation-errorBackground` | `showError()` banner | Transient error |

## Fallbacks

`.banner.info` provides hex fallbacks because `--vscode-editorInfo-*` is not
defined in every theme:

```css
background:  var(--vscode-editorInfo-background, #e8f4fd);
color:       var(--vscode-editorInfo-foreground, #014361);
border-left: 4px solid var(--vscode-editorInfo-border, #2196f3);
```

No other selector currently provides fallbacks. Add them only when a
specific theme is observed to render an unstyled element — do not pre-empt.

## High contrast

VS Code high-contrast themes typically swap `--vscode-foreground` and panel
backgrounds to pure white/black with strong borders. Our reliance on
`--vscode-panel-border` for section separators means the layout remains
readable; the only attention point is the `opacity: 0.7` on `<h2>` and
`opacity: 0.6` on the instructions sub-text — these may dim text below
high-contrast contrast minimums.

Future improvement: replace opacity-based de-emphasis with
`--vscode-descriptionForeground` (a dedicated muted-text token).

## Typography

- Body inherits `--vscode-font-family`, no explicit size — uses VS Code's
  computed default.
- Section titles: `font-size: 13px; text-transform: uppercase`.
- Sub-labels (instructions paths): `font-size: 11px`.
- Hints list: `font-size: 12px`.
- Overall health score: `font-size: 24px` (inline style).

The 24 px inline size for the health score is a deliberate emphasis. All
other sizes stay close to the editor body size.

## Iconography

- Zone icons are emoji literals (🟢 🟡 🔴), not theme tokens. They render
  identically across themes but do not adapt to color-blind preferences.
  A future enhancement would be SVG codicon glyphs colored via theme tokens
  (`codicon-pass-filled`, `codicon-warning`, `codicon-error`).
- Source badges use a `●` Unicode bullet, also theme-independent.
- Install status icons (✅ / ⚠️) are emoji literals on the instructions row.

## Content Security Policy

Set in `html.ts`:

```
default-src 'none';
img-src    ${webview.cspSource};
style-src  ${webview.cspSource};
script-src 'nonce-${nonce}';
```

Implications for design:

- All styles must come from `media/panel.css` (no inline `<style>`).
- All scripts must be the bundled `media/panel.js` with the matching nonce.
- Inline styles set via `element.style.cssText = "…"` are allowed (CSP only
  blocks inline `<style>` blocks and event handlers, not the DOM style API).
- No external fonts, no remote images.

If a future design needs an icon font or codicon, it must ship inside the
extension bundle and be referenced via a relative `webview.asWebviewUri(...)`
path so the CSP `style-src ${webview.cspSource}` directive permits it.
