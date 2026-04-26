# Step 02 — .vscodeignore for per-platform packaging

## Goal
Single `.vscodeignore` lists everything excluded; per-target packaging command additionally excludes the other-platform binaries via `--ignoreFile`.

## File: `vscode-extension/.vscodeignore`

```
.vscode/**
.vscode-test/**
src/**
node_modules/**
out/**.map
**/*.ts
**/*.tsbuildinfo
tsconfig.json
esbuild.mjs
.eslintrc.json
.gitignore
scripts/**
test-logs/**
*.vsix
```

## Per-target ignore files

`vscode-extension/.vscodeignore.win32-x64`:
```
bin/linux-x64/**
bin/darwin-arm64/**
```

`vscode-extension/.vscodeignore.linux-x64`:
```
bin/win32-x64/**
bin/darwin-arm64/**
```

`vscode-extension/.vscodeignore.darwin-arm64`:
```
bin/win32-x64/**
bin/linux-x64/**
```

These are passed to `vsce package` via `--ignoreFile` in step 03.

## Done when
- [ ] `.vscodeignore` excludes source/dev files
- [ ] Three per-target ignore files exist with correct exclusions
