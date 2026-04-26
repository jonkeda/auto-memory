#!/usr/bin/env pwsh
# Verify that net/Version.props matches pyproject.toml version

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# Extract version from pyproject.toml
$pyprojectPath = Join-Path $repoRoot "pyproject.toml"
if (-not (Test-Path $pyprojectPath)) {
    Write-Error "pyproject.toml not found at: $pyprojectPath"
    exit 1
}

$pyprojectContent = Get-Content $pyprojectPath -Raw
if ($pyprojectContent -match 'version\s*=\s*"([^"]+)"') {
    $pyVersion = $Matches[1]
} else {
    Write-Error "Could not extract version from pyproject.toml"
    exit 1
}

# Extract version from Version.props
$versionPropsPath = Join-Path $repoRoot "net" "Version.props"
if (-not (Test-Path $versionPropsPath)) {
    Write-Error "Version.props not found at: $versionPropsPath"
    exit 1
}

[xml]$versionProps = Get-Content $versionPropsPath
$dotnetVersion = $versionProps.Project.PropertyGroup.Version

if ([string]::IsNullOrWhiteSpace($dotnetVersion)) {
    Write-Error "Could not extract <Version> from Version.props"
    exit 1
}

# Compare
if ($pyVersion -ne $dotnetVersion) {
    Write-Error @"
Version mismatch detected:
  pyproject.toml: $pyVersion
  Version.props:  $dotnetVersion

Please update net/Version.props to match pyproject.toml
"@
    exit 1
}

Write-Host "✓ Version parity verified: $pyVersion" -ForegroundColor Green
exit 0
