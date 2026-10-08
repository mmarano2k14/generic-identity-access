[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "MFA parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Needle)) { throw "MFA parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.Contains($Needle)) { throw "MFA parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

foreach ($serverModule in @(
    'packages/next/src/server/mfa-workspace.ts',
    'packages/next/src/server/mfa-mutations.ts'
)) {
    Require-Text $serverModule 'import "server-only";'
}

foreach ($marker in @(
    'loadNextMfaWorkspace',
    'NextMfaWorkspacePermissions',
    'mfa-policy',
    'mfa-authenticator',
    'canReadUsers',
    'listProviders',
    'getPolicy',
    'getUserSecurityState',
    'listAuthenticators'
)) { Require-Text 'packages/next/src/server/mfa-workspace.ts' $marker }

foreach ($marker in @(
    'createNextMfaPolicyFromForm',
    'updateNextMfaPolicyFromForm',
    'revokeNextMfaAuthenticatorFromForm',
    'recoveryRevokeNextMfaAuthenticatorFromForm',
    'confirmation must be'
)) { Require-Text 'packages/next/src/server/mfa-mutations.ts' $marker }

foreach ($marker in @(
    'loadNextMfaWorkspace',
    'createNextMfaPolicyFromForm',
    'recoveryRevokeNextMfaAuthenticatorFromForm'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

Require-Text 'packages/react/src/security-operations/MfaPolicyForm.tsx' 'typeof formAction === "function"'
Require-Text 'packages/react/src/security-operations/MfaPolicyForm.tsx' 'allowedProviders'
Require-Text 'packages/react/src/security-operations/AuthenticatorRevocationForm.tsx' 'Type REVOKE to confirm'
Require-Text 'packages/react/src/security-operations/AuthenticatorRevocationForm.tsx' 'typeof formAction === "function"'
Require-Text 'packages/react/src/pages/MfaPage.tsx' 'Inspect effective MFA state'
Require-Text 'packages/react/src/pages/MfaPage.tsx' 'Provider secrets are never rendered'
Require-Text 'packages/react/src/pages/MfaPage.tsx' 'selectedUser'
Reject-Text 'packages/react/src/security-operations/AuthenticatorRevocationForm.tsx' 'name="userId" required'

$testPath = Require-File 'packages/next/test/mfa-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'MFA SDK behavior tests failed.' }

Write-Host 'Generic Identity MFA GOLDEN parity source/behavior validation: GREEN'
