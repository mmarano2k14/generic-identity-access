[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Sessions parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Needle)) { throw "Sessions parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.Contains($Needle)) { throw "Sessions parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

foreach ($serverModule in @(
    'packages/next/src/server/session-security-workspace.ts',
    'packages/next/src/server/session-security-mutations.ts'
)) {
    Require-Text $serverModule 'import "server-only";'
}

foreach ($marker in @(
    'loadNextSessionSecurityWorkspace',
    'never infers active/expired/revoked session state',
    'PasswordLoginSucceeded',
    'OidcRefreshTokenReuseDetected',
    'security-audit',
    'session',
    'canWriteSessions',
    'evidenceState'
)) { Require-Text 'packages/next/src/server/session-security-workspace.ts' $marker }

foreach ($marker in @(
    'revokeNextUserSessionsFromForm',
    'revokeNextClientSessionsFromForm',
    'confirmation must be'
)) { Require-Text 'packages/next/src/server/session-security-mutations.ts' $marker }

foreach ($marker in @(
    'loadNextSessionSecurityWorkspace',
    'revokeNextUserSessionsFromForm',
    'revokeNextClientSessionsFromForm'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

foreach ($marker in @(
    'This is not an inferred active-session inventory',
    'Session issuance',
    'Revocation activity',
    'Continuity alerts',
    'Server-confirmed revocation operations'
)) { Require-Text 'packages/react/src/security-operations/SessionSecurityPage.tsx' $marker }

foreach ($marker in @(
    'IdentityEntityAutocomplete',
    'kind="user"',
    'Client ID',
    'All outcomes',
    '100 events'
)) { Require-Text 'packages/react/src/security-operations/SessionSecurityFilterForm.tsx' $marker }

foreach ($marker in @(
    'Type REVOKE to confirm',
    'typeof formAction === "function"',
    'IdentityEntityAutocomplete',
    'kind="user"'
)) { Require-Text 'packages/react/src/security-operations/SessionRevocationForm.tsx' $marker }

Reject-Text 'packages/react/src/security-operations/SessionSecurityPage.tsx' 'sessionToken'
Reject-Text 'packages/react/src/security-operations/SessionSecurityPage.tsx' 'refreshToken'
Reject-Text 'packages/react/src/security-operations/SessionSecurityPage.tsx' 'activeSessions'

$testPath = Require-File 'packages/next/test/session-security-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'Sessions SDK behavior tests failed.' }

Write-Host 'Generic Identity Sessions GOLDEN parity source/behavior validation: GREEN'
