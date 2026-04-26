# Step 03 — Local build script

## File: `vscode-extension/scripts/build-all.ps1`

```powershell
param([string]$OutDir = 'dist')

Push-Location $PSScriptRoot/..
npm run sync-version
npm run build

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$targets = @('win32-x64','linux-x64','darwin-arm64')
foreach ($t in $targets) {
    Write-Host "Packaging $t..."
    npx vsce package --target $t --ignoreFile ".vscodeignore.$t" --out $OutDir
}
Pop-Location
Get-ChildItem $OutDir -Filter *.vsix | Format-Table Name, Length
```

## Bash equivalent: `vscode-extension/scripts/build-all.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail
OUT_DIR="${1:-dist}"
cd "$(dirname "$0")/.."
npm run sync-version
npm run build
mkdir -p "$OUT_DIR"
for t in win32-x64 linux-x64 darwin-arm64; do
  echo "Packaging $t..."
  npx vsce package --target "$t" --ignoreFile ".vscodeignore.$t" --out "$OUT_DIR"
done
ls -la "$OUT_DIR"/*.vsix
```

## Done when
- [ ] Running the script produces 3 `.vsix` files in `dist/`
- [ ] Each VSIX contains only its target-platform binary (verify with `unzip -l`)
- [ ] Versions in filenames match `Version.props`
