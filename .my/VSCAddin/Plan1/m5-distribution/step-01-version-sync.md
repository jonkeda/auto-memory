# Step 01 — Version sync

## File: `vscode-extension/scripts/sync-version.mjs`

```javascript
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const propsPath = join(here, '..', '..', 'net', 'Version.props');
const pkgPath   = join(here, '..', 'package.json');

const props = await readFile(propsPath, 'utf8');
const m = props.match(/<VersionPrefix>([\d.]+)<\/VersionPrefix>/);
if (!m) { console.error('VersionPrefix not found'); process.exit(1); }
const version = m[1];

const pkg = JSON.parse(await readFile(pkgPath, 'utf8'));
if (pkg.version === version) {
  console.log(`package.json already at ${version}`);
} else {
  pkg.version = version;
  await writeFile(pkgPath, JSON.stringify(pkg, null, 2) + '\n');
  console.log(`package.json synced to ${version}`);
}
```

## Wire into npm scripts

In `package.json`:

```json
"scripts": {
  "sync-version": "node scripts/sync-version.mjs",
  "build": "npm run sync-version && node esbuild.mjs",
  "package": "npm run sync-version && vsce package"
}
```

## Done when
- [ ] `npm run sync-version` updates `package.json` to match `Version.props`
- [ ] No-op message when versions already match
- [ ] Script exits with code 1 if `VersionPrefix` is missing
