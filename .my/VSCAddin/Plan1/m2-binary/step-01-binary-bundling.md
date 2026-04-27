# Step 01 — Binary bundling

## Goal
Lay out per-platform binaries under `bin/` and update `.vscodeignore` to keep packaging selective.

## Layout

```
vscode-extension/
  bin/
    win32-x64/    session-recall.exe
    linux-x64/    session-recall
    darwin-arm64/ session-recall
```

## How binaries are produced

- Built from `net/AutoMemory.Cli` via `dotnet publish -r <rid> -c Release -p:PublishSingleFile=true --self-contained`
- The vscode-extension repo does **not** rebuild them; CI copies them from the .NET build artefact (M5 wires this).
- For local development, copy from `net/publish-<rid>/session-recall[.exe]` into `bin/<platform>/`.

## Edits

- Add `bin/` to extension package; `.vscodeignore` should NOT exclude `bin/`.
- Add a placeholder README in each empty platform folder so directories exist in git: `bin/<platform>/.gitkeep`.

## Done when
- [ ] `bin/win32-x64/`, `bin/linux-x64/`, `bin/darwin-arm64/` directories exist with `.gitkeep`
- [ ] Local copy of `session-recall.exe` placed in `bin/win32-x64/` for development
- [ ] `npm run package` does not exclude `bin/` from the VSIX
