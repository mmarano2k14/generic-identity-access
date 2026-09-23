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

$postgreSqlDirectory = Join-Path $PSScriptRoot "postgresql"

$databaseGates = @(
    "verify-migration-integrity.ps1",
    "verify-default-database.ps1",
    "verify-directory-persistence.ps1",
    "verify-permission-persistence.ps1",
    "verify-assigned-capability-projection.ps1",
    "verify-rbac-capability-alignment.ps1",
    "verify-wildcard-policy-patterns.ps1",
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

Write-Host "Running repository build, architecture, and unit/integration test gates..."
& (Join-Path $PSScriptRoot "verify.ps1") -Configuration $Configuration

Write-Host "Running external RBAC compatibility qualification..."
& (Join-Path $PSScriptRoot "verify-multiplexed-rbac.ps1") `
    -ReferenceDirectory $RbacReferenceDirectory

foreach ($gate in $databaseGates) {
    Write-Host "Running PostgreSQL qualification gate: $gate"
    & (Join-Path $postgreSqlDirectory $gate)
}

if (-not $SkipBackupRestore) {
    Write-Host "Running disposable backup/restore qualification..."
    & (Join-Path $postgreSqlDirectory "verify-backup-restore.ps1")
} else {
    Write-Warning "Backup/restore qualification was explicitly skipped; qualification is partial."
}

Write-Host "Production qualification gates completed successfully."
