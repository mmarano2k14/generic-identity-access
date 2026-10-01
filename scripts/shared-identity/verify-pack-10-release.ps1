[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$qualifier = Join-Path $root 'scripts/shared-identity/qualify-release-packages.mjs'
if (-not (Test-Path $qualifier -PathType Leaf)) {
    throw 'Shared Identity Pack 10 release qualifier is missing.'
}

foreach ($packageName in @('contracts', 'auth', 'react', 'next')) {
    $packagePath = Join-Path $root "packages/$packageName/package.json"
    $manifest = Get-Content $packagePath -Raw | ConvertFrom-Json
    if ([string]$manifest.version -ne '1.0.0') {
        throw "Shared Identity release package '$packageName' must be version 1.0.0."
    }
    if ($manifest.private -ne $true) {
        throw "Repository source package '$packageName' must remain private to prevent accidental direct publication."
    }
    if ([string]$manifest.publishConfig.access -ne 'public') {
        throw "Shared Identity release package '$packageName' must declare publishConfig.access=public."
    }
}

$builderPath = Join-Path $root 'scripts/shared-identity/build-local-consumer-packages.mjs'
$builderSource = [System.IO.File]::ReadAllText($builderPath)
foreach ($marker in @(
    'stageClientPackageJson.private = false',
    'stageClientPackageJson.publishConfig = { access: "public" }',
    'packageJson.private = false',
    'packageJson.publishConfig = { access: "public" }',
    'Restoring legacy TypeScript client development dependencies after the initial build could not resolve its toolchain'
)) {
    if (-not $builderSource.Contains($marker)) {
        throw "Shared Identity Pack 10 artifact builder is missing release marker '$marker'."
    }
}

$qualifierSource = [System.IO.File]::ReadAllText($qualifier)
foreach ($marker in @(
    'const releaseVersion = "1.0.0"',
    'qualified-not-published',
    'publishConfig?.access !== "public"',
    'release-manifest.json',
    'Release artifact checksum mismatch'
)) {
    if (-not $qualifierSource.Contains($marker)) {
        throw "Shared Identity Pack 10 qualifier is missing marker '$marker'."
    }
}

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    throw 'Node.js is required for Shared Identity Pack 10 release artifact qualification.'
}
& node $qualifier
if ($LASTEXITCODE -ne 0) {
    throw 'Shared Identity Pack 10 release artifact qualification failed.'
}

$releaseManifest = Join-Path $root 'artifacts/shared-identity-release/release-manifest.json'
if (-not (Test-Path $releaseManifest -PathType Leaf)) {
    throw 'Shared Identity Pack 10 release manifest was not generated.'
}

Write-Host 'Shared Identity Pack 10 release packaging validation: GREEN'
