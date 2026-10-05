[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Security Operations required file is missing: $RelativePath"
    }
    return $path
}

function Read-Source([string]$RelativePath) {
    return [System.IO.File]::ReadAllText((Require-File $RelativePath))
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    if (-not (Read-Source $RelativePath).Contains($Needle)) {
        throw "Security Operations file '$RelativePath' is missing marker '$Needle'."
    }
}

$categoryFiles = @(
    'packages/contracts/src/security-operations/index.ts',
    'packages/contracts/test/consumer-security-operations.ts',
    'packages/auth/src/security-operations.ts',
    'packages/auth/test/consumer-security-operations.ts',
    'packages/react/src/security-operations/index.ts',
    'packages/react/src/security-operations/SecurityAuditPage.tsx',
    'packages/react/src/security-operations/MfaPolicyForm.tsx',
    'packages/react/src/security-operations/AuthenticatorRevocationForm.tsx',
    'packages/react/src/security-operations/SessionRevocationForm.tsx',
    'packages/react/test/consumer-security-operations.tsx',
    'packages/next/src/security-operations/index.ts',
    'packages/next/test/consumer-security-operations.tsx',
    'docs/shared-identity/SECURITY_OPERATIONS.md',
    'docs/shared-identity/changes/1.5.0-security-operations.md'
)

foreach ($file in $categoryFiles) {
    [void](Require-File $file)
}

Require-Text 'packages/auth/src/client.ts' 'readonly security: GenericIdentitySecurityOperationsClient;'
Require-Text 'packages/auth/src/client.ts' 'createGenericIdentitySecurityOperationsClient('
Require-Text 'packages/auth/src/security-operations.ts' 'readonly sessions: GenericIdentitySecuritySessionsClient;'
Require-Text 'packages/auth/src/security-operations.ts' 'readonly mfa: GenericIdentitySecurityMfaClient;'
Require-Text 'packages/auth/src/security-operations.ts' 'readonly audit: GenericIdentitySecurityAuditClient;'
Require-Text 'packages/auth/src/security-operations.ts' 'revokeAuthenticatorForRecovery('
Require-Text 'packages/auth/src/security-operations.ts' 'revokeClient('
Require-Text 'packages/auth/src/security-operations.ts' 'IdentitySecurityAuditQuery'

Require-Text 'packages/contracts/package.json' '"./security-operations"'
Require-Text 'packages/auth/package.json' '"./security-operations"'
Require-Text 'packages/react/package.json' '"./security-operations"'
Require-Text 'packages/next/package.json' '"./security-operations"'

Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'GenericIdentitySecurityOperationsClient.mfa.listProviders'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'GenericIdentitySecurityOperationsClient.sessions revokeUser/revokeClient'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'GenericIdentitySecurityOperationsClient.audit.list'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'TOTP enrollment/confirmation'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' '`BACKEND_API_MISSING`'
Require-Text 'docs/shared-identity/SECURITY_OPERATIONS.md' 'No SDK method, mock route or speculative contract is added for them.'

$securitySource = Read-Source 'packages/auth/src/security-operations.ts'
foreach ($forbidden in @('beginTotpEnrollment', 'confirmTotpEnrollment', 'beginWebAuthnRegistration', 'completeWebAuthnRegistration', 'generateRecoveryCodes')) {
    if ($securitySource.Contains($forbidden)) {
        throw "Security Operations SDK must not expose backend-missing operation '$forbidden'."
    }
}

$versions = @()
foreach ($packageName in @('contracts', 'auth', 'react', 'next')) {
    $manifestPath = Join-Path $root "packages/$packageName/package.json"
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $version = [version][string]$manifest.version
    if ($version -lt [version]'1.5.0') {
        throw "Security Operations package '$packageName' must remain at or above version 1.5.0."
    }
    $versions += $version.ToString()
}

$alignedVersions = @($versions | Sort-Object -Unique)
if ($alignedVersions.Count -ne 1) {
    throw 'Generic Identity public package versions must remain aligned.'
}

foreach ($file in $categoryFiles) {
    $source = [System.IO.File]::ReadAllText((Join-Path $root $file))
    if ($source.IndexOf('consumer-app', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Consumer-neutral Security Operations source '$file' must not contain a consumer product name."
    }
}

$changeRecord = Read-Source 'docs/shared-identity/changes/1.5.0-security-operations.md'
foreach ($marker in @('## Added', '## Modified', '## Moved', '## Deleted', '## Database migrations', '## Runtime changes', '## Breaking changes', '## Post-apply validation')) {
    if (-not $changeRecord.Contains($marker)) {
        throw "Security Operations change record is missing section '$marker'."
    }
}

Write-Host 'Generic Identity Security Operations source validation: GREEN'
