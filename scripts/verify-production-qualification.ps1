[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RbacReferenceDirectory,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipBackupRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------
# Resolve repository paths
# ---------------------------------------------------------------------------

$postgreSqlDirectory = Join-Path $PSScriptRoot "postgresql"

if (-not (Test-Path -LiteralPath $postgreSqlDirectory -PathType Container)) {
    throw "PostgreSQL scripts directory does not exist: $postgreSqlDirectory"
}

# ---------------------------------------------------------------------------
# Local PostgreSQL password file
#
# scripts/postgresql/.pgpass.local is intentionally local and gitignored.
# Child PowerShell/psql processes inherit PGPASSFILE automatically.
# ---------------------------------------------------------------------------

$localPgPass = Join-Path $postgreSqlDirectory ".pgpass.local"

if (Test-Path -LiteralPath $localPgPass -PathType Leaf) {
    $env:PGPASSFILE = (Resolve-Path -LiteralPath $localPgPass).Path
    Write-Host "Using local PostgreSQL password file: $env:PGPASSFILE"
}

# ---------------------------------------------------------------------------
# Resolve external RBAC reference
#
# Trim accidental whitespace or surrounding quotes from command-line input.
# ---------------------------------------------------------------------------

$RbacReferenceDirectory = $RbacReferenceDirectory.Trim()
$RbacReferenceDirectory = $RbacReferenceDirectory.Trim('"').Trim("'")

if (-not (Test-Path -LiteralPath $RbacReferenceDirectory -PathType Container)) {
    throw "RBAC reference directory does not exist: $RbacReferenceDirectory"
}

$resolvedRbacReferenceDirectory =
    (Resolve-Path -LiteralPath $RbacReferenceDirectory).Path

Write-Host "RBAC reference directory: $resolvedRbacReferenceDirectory"

# ---------------------------------------------------------------------------
# Qualification gates
# ---------------------------------------------------------------------------

$databaseGates = @(
    "verify-migration-integrity.ps1",
    "verify-default-database.ps1",
    "verify-directory-persistence.ps1",
    "verify-assigned-capability-projection.ps1",
    "verify-rbac-capability-alignment.ps1",
    "verify-resource-scope-hierarchy.ps1",
    "verify-authentication-foundation.ps1",
    "verify-session-lifecycle.ps1",
    "verify-security-audit.ps1",
    "verify-identity-scope-authority.ps1",
    "verify-identity-scope-authority-administration.ps1",
    "verify-transactional-security-audit.ps1",
    "verify-atomic-mutations.ps1",
    "verify-oidc-authorization-code.ps1",
    "verify-oidc-refresh-token.ps1",
    "verify-mfa-provider-foundation.ps1"
)

# ---------------------------------------------------------------------------
# Repository verification
# ---------------------------------------------------------------------------

$repositoryVerifier = Join-Path $PSScriptRoot "verify.ps1"

if (-not (Test-Path -LiteralPath $repositoryVerifier -PathType Leaf)) {
    throw "Repository verifier does not exist: $repositoryVerifier"
}

Write-Host "Running repository build, architecture, and unit/integration test gates..."

& $repositoryVerifier -Configuration $Configuration

# ---------------------------------------------------------------------------
# External RBAC compatibility
# ---------------------------------------------------------------------------

$rbacVerifier = Join-Path $PSScriptRoot "verify-multiplexed-rbac.ps1"

if (-not (Test-Path -LiteralPath $rbacVerifier -PathType Leaf)) {
    throw "RBAC verifier does not exist: $rbacVerifier"
}

Write-Host "Running external RBAC compatibility qualification..."

$rbacArguments = @{
    ReferenceDirectory = $resolvedRbacReferenceDirectory
}

& $rbacVerifier @rbacArguments

# ---------------------------------------------------------------------------
# PostgreSQL qualification
# ---------------------------------------------------------------------------

foreach ($gate in $databaseGates) {
    $gatePath = Join-Path $postgreSqlDirectory $gate

    if (-not (Test-Path -LiteralPath $gatePath -PathType Leaf)) {
        throw "PostgreSQL qualification gate does not exist: $gatePath"
    }

    Write-Host "Running PostgreSQL qualification gate: $gate"

    & $gatePath
}

# ---------------------------------------------------------------------------
# Backup / restore qualification
# ---------------------------------------------------------------------------

if (-not $SkipBackupRestore) {
    $backupRestoreVerifier =
        Join-Path $postgreSqlDirectory "verify-backup-restore.ps1"

    if (-not (Test-Path -LiteralPath $backupRestoreVerifier -PathType Leaf)) {
        throw "Backup/restore verifier does not exist: $backupRestoreVerifier"
    }

    Write-Host "Running disposable backup/restore qualification..."

    & $backupRestoreVerifier
}
else {
    Write-Warning "Backup/restore qualification was explicitly skipped; qualification is partial."
}

Write-Host "Production qualification gates completed successfully."