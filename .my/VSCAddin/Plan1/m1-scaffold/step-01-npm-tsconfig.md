# Step 01 — npm + tsconfig + esbuild

## Goal
Bootstrap `vscode-extension/` with Node tooling, TypeScript, esbuild bundler, ESLint, and the standard `.vscodeignore`.

## Files to create

- `vscode-extension/package.json` — minimal (no contributions yet; step 02 fills these in)
- `vscode-extension/tsconfig.json` — `target: ES2022`, `module: commonjs`, `outDir: out`, `strict: true`
- `vscode-extension/esbuild.mjs` — bundles `src/extension.ts` → `out/extension.js` (cjs, node, external: vscode)
- `vscode-extension/.eslintrc.json` — `@typescript-eslint/recommended`
- `vscode-extension/.vscodeignore` — exclude `src/`, `node_modules/`, `*.ts`, `tsconfig.json`, `esbuild.mjs`, `.eslintrc.json`
- `vscode-extension/.gitignore` — `node_modules/`, `out/`, `dist/`, `*.vsix`

## package.json (minimal)

```json
{
  "name": "auto-memory",
  "displayName": "Auto Memory",
  "publisher": "auto-memory",
  "version": "0.1.0",
  "engines": { "vscode": "^1.90.0" },
  "main": "./out/extension.js",
  "scripts": {
    "build": "node esbuild.mjs",
    "watch": "node esbuild.mjs --watch",
    "lint": "eslint src --ext ts",
    "package": "vsce package"
  },
  "devDependencies": {
    "@types/node": "^20.0.0",
    "@types/vscode": "^1.90.0",
    "@typescript-eslint/eslint-plugin": "^7.0.0",
    "@typescript-eslint/parser": "^7.0.0",
    "esbuild": "^0.20.0",
    "eslint": "^8.57.0",
    "typescript": "^5.4.0",
    "@vscode/vsce": "^2.24.0"
  }
}
```

## Done when
- [ ] `cd vscode-extension && npm install` succeeds
- [ ] `npm run build` produces `out/extension.js` (will be a stub at this step — extension.ts created in step 03)
- [ ] `npm run lint` passes
