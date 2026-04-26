import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const propsPath = join(here, '..', '..', 'net', 'Version.props');
const pkgPath   = join(here, '..', 'package.json');

const props = await readFile(propsPath, 'utf8');
const m = props.match(/<Version>([\d.]+)<\/Version>/);
if (!m) { console.error('Version not found'); process.exit(1); }
const version = m[1];

const pkg = JSON.parse(await readFile(pkgPath, 'utf8'));
if (pkg.version === version) {
  console.log(`package.json already at ${version}`);
} else {
  pkg.version = version;
  await writeFile(pkgPath, JSON.stringify(pkg, null, 2) + '\n');
  console.log(`package.json synced to ${version}`);
}
