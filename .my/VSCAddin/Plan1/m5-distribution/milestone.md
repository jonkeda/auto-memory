# M5 — Packaging and distribution

**Goal:** Signed, per-platform VSIX packages are published to the VS Code Marketplace and Open VSX Registry on every release tag. Version is synced from the single source of truth in `net/Version.props`.

**Depends on:** M1–M4 all complete and passing

---

## Deliverables

- Per-platform VSIX build script (`vscode-extension/scripts/build.ps1` or `Makefile`)
- Version sync script: reads `net/Version.props`, writes `vscode-extension/package.json`
- CI workflow: `.github/workflows/vscode-ext.yml`
- Code-signed VSIX (publisher cert in GitHub secret)
- Published to VS Code Marketplace
- Published to Open VSX Registry
- Release notes attached to GitHub release alongside CLI binaries

---

## Version sync

`net/Version.props` is the single source of truth:

```xml
<Project>
  <PropertyGroup>
    <VersionPrefix>0.1.0</VersionPrefix>
  </PropertyGroup>
</Project>
```

A prebuild script (`vscode-extension/scripts/sync-version.mjs`) reads the XML and patches `package.json`:

```js
// reads VersionPrefix from Version.props, writes to package.json "version"
```

Run as part of `npm run build` and as the first CI step.

---

## Per-platform VSIX

Three packages built, one per target platform:

| File | `--target` flag |
|---|---|
| `auto-memory-0.1.0-win32-x64.vsix` | `win32-x64` |
| `auto-memory-0.1.0-linux-x64.vsix` | `linux-x64` |
| `auto-memory-0.1.0-darwin-arm64.vsix` | `darwin-arm64` |

Each package includes only the binary for its platform (`.vscodeignore` excludes the other `bin/` subdirectories).

Build command:

```bash
vsce package --target win32-x64   --out dist/
vsce package --target linux-x64   --out dist/
vsce package --target darwin-arm64 --out dist/
```

---

## CI workflow (`.github/workflows/vscode-ext.yml`)

```yaml
on:
  push:
    tags: ['v*']

jobs:
  build:
    strategy:
      matrix:
        target: [win32-x64, linux-x64, darwin-arm64]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd vscode-extension && npm ci
      - run: node vscode-extension/scripts/sync-version.mjs
      - run: npx vsce package --target ${{ matrix.target }} --out dist/
      - uses: actions/upload-artifact@v4
        with:
          name: vsix-${{ matrix.target }}
          path: dist/*.vsix

  publish:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with: { path: dist/ }
      - name: Publish to Marketplace
        run: |
          for f in dist/**/*.vsix; do
            npx vsce publish --packagePath "$f" --pat ${{ secrets.VSCE_PAT }}
          done
      - name: Publish to Open VSX
        run: |
          for f in dist/**/*.vsix; do
            npx ovsx publish "$f" --pat ${{ secrets.OVSX_PAT }}
          done
      - name: Attach to GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: dist/**/*.vsix
```

---

## Signing

- Publisher certificate stored as `VSCE_PAT` GitHub secret (VS Code Marketplace PAT)
- Open VSX token stored as `OVSX_PAT`
- `vsce` handles signing for Marketplace; Open VSX uses token auth only

---

## `.vscodeignore`

```
bin/win32-x64/**    (excluded in linux/darwin packages)
bin/linux-x64/**    (excluded in win/darwin packages)
bin/darwin-arm64/** (excluded in win/linux packages)
src/**
scripts/**
*.ts
tsconfig.json
.eslintrc*
node_modules/**
```

Each platform matrix build sets which `bin/<other-platform>` entries to ignore before packaging.

---

## Release checklist

- [ ] `net/Version.props` bumped to new version
- [ ] `sync-version.mjs` run: `package.json` version matches
- [ ] `CHANGELOG.md` entry added in `vscode-extension/`
- [ ] All M1–M4 acceptance criteria green on CI
- [ ] Tag pushed: `git tag v0.x.0 && git push origin v0.x.0`
- [ ] CI publishes three VSIX files to Marketplace and Open VSX
- [ ] Verify each platform VSIX installs cleanly from the Marketplace
- [ ] VSIX files attached to GitHub Release alongside CLI binaries (`session-recall-linux-x64`, etc.)

---

## Acceptance criteria

- [ ] `vsce package --target win32-x64` produces a valid VSIX containing `bin/win32-x64/session-recall.exe` and no other platform binaries
- [ ] Same for linux-x64 and darwin-arm64
- [ ] CI workflow triggers on `v*` tag and completes without errors
- [ ] Extension appears on VS Code Marketplace search within 5 minutes of publish
- [ ] Extension appears on Open VSX Registry
- [ ] Installing the win32-x64 VSIX on Windows extracts the binary and adds it to PATH without errors
- [ ] Version in Marketplace matches `net/Version.props`
