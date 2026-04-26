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
