# dev-package.ps1
# Builds the session-recall binary for the current platform and packages a local VSIX.
# Usage: pwsh scripts/dev-package.ps1 [-OutDir dist]
param([string]$OutDir = 'dist')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root      = (Resolve-Path "$PSScriptRoot/../..").Path      # repo root
$extRoot   = (Resolve-Path "$PSScriptRoot/..").Path         # vscode-extension/
$netCli    = Join-Path $root 'net' 'src' 'AutoMemory.Cli'
$rid       = 'win-x64'
$target    = 'win32-x64'
$publishDir= Join-Path $root "net" "publish-$rid"
$binDst    = Join-Path $extRoot "bin" $target
$exeName   = 'session-recall.exe'

Write-Host "==> Building .NET CLI ($rid)..." -ForegroundColor Cyan
dotnet publish $netCli -c Release -r $rid --self-contained `
    -p:PublishSingleFile=true `
    -o $publishDir `
    --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "==> Copying binary to bin/$target/ ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $binDst -Force | Out-Null
Copy-Item (Join-Path $publishDir $exeName) (Join-Path $binDst $exeName) -Force

Write-Host "==> Syncing version from Version.props ..." -ForegroundColor Cyan
Push-Location $extRoot
node scripts/sync-version.mjs

Write-Host "==> Building TypeScript ..." -ForegroundColor Cyan
npm run build --if-present

Write-Host "==> Packaging VSIX ($target) ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path (Join-Path $extRoot $OutDir) -Force | Out-Null
npx vsce package --target $target `
    --ignoreFile ".vscodeignore.$target" `
    --out $OutDir
Pop-Location

Write-Host ""
Write-Host "==> Done:" -ForegroundColor Green
Get-ChildItem (Join-Path $extRoot $OutDir) -Filter *.vsix | ForEach-Object {
    Write-Host "    $($_.FullName)  ($([math]::Round($_.Length/1MB,2)) MB)" -ForegroundColor Green
}
