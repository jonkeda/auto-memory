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
