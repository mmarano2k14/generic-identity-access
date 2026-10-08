[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Security Audit parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Needle)) { throw "Security Audit parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.Contains($Needle)) { throw "Security Audit parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

Require-Text 'packages/next/src/server/security-audit-workspace.ts' 'import "server-only";'
foreach ($marker in @(
    'loadNextSecurityAuditWorkspace',
    'normalizeNextSecurityAuditQuery',
    'summarizeSecurityAudit',
    'security-audit',
    'correlationId',
    'limit: query.limit'
)) { Require-Text 'packages/next/src/server/security-audit-workspace.ts' $marker }

foreach ($marker in @(
    'loadNextSecurityAuditWorkspace',
    'NextSecurityAuditWorkspace',
    'NextSecurityAuditSummary'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

foreach ($marker in @(
    'IdentityEntityAutocomplete',
    'kind="user"',
    'kind="tenant"',
    'All outcomes',
    'Correlation ID',
    '200 events'
)) { Require-Text 'packages/react/src/security-operations/SecurityAuditFilterForm.tsx' $marker }

foreach ($marker in @(
    'Read-only application-scoped evidence',
    'Visible events',
    'Succeeded',
    'Denied',
    'Failed',
    'gi-audit-timeline',
    'reasonCode',
    'correlationHref'
)) { Require-Text 'packages/react/src/security-operations/SecurityAuditPage.tsx' $marker }

Reject-Text 'packages/react/src/security-operations/SecurityAuditPage.tsx' 'sessionToken'
Reject-Text 'packages/react/src/security-operations/SecurityAuditPage.tsx' 'refreshToken'
Reject-Text 'packages/react/src/security-operations/SecurityAuditPage.tsx' 'passwordHash'

$testPath = Require-File 'packages/next/test/security-audit-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'Security Audit SDK behavior tests failed.' }

Write-Host 'Generic Identity Security Audit GOLDEN parity source/behavior validation: GREEN'
