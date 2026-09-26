[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required managed-policy publication source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$version = Read-Source "src/IdentityAccess.Domain/ManagedPolicyVersion.cs"
if ($version -notmatch 'PublishedAt' -or $version -notmatch 'IsPublished') {
    throw "Managed policy versions must expose explicit publication state."
}

$contract = Read-Source "src/IdentityAccess.Application/Storage/IManagedPolicyVersionStore.cs"
if ($contract -notmatch 'PublishAsync') {
    throw "Managed policy version persistence must expose an explicit publication operation."
}

$migration = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0025_managed_policy_publication.sql"
foreach ($required in @(
    'published_at',
    'SET published_at = pv.created_at',
    'managed_group_policy_bindings',
    'guard_managed_policy_version_mutation',
    'guard_managed_policy_statement_mutation',
    'guard_managed_policy_default_version',
    'guard_managed_policy_binding_publication')) {
    if ($migration -notmatch [regex]::Escape($required)) {
        throw "Managed policy publication migration is missing required protection: $required"
    }
}

$statementStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedPolicyStatementStore.cs"
if ($statementStore -notmatch 'pv\.published_at IS NULL') {
    throw "Managed policy statement writes must be restricted to unpublished versions."
}

$bindingMutation = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedGroupPolicyBindingMutationStore.cs"
if ($bindingMutation -notmatch 'pv\.published_at IS NOT NULL') {
    throw "Managed policy binding creation must require a published version."
}

$grant = Read-Source "src/IdentityAccess.Application/Authorization/AssignedCapabilityGrant.cs"
if ($grant -notmatch 'ManagedPolicyVersionReference ManagedPolicyVersion' -or $grant -match 'PermissionPolicyReference') {
    throw "Authorization grant provenance must be managed-policy-version only."
}

$reader = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlAssignedCapabilityReader.cs"
foreach ($required in @(
    'managed_group_policy_bindings',
    'managed_policy_statements',
    'mpv.published_at IS NOT NULL',
    'new ManagedPolicyVersionReference')) {
    if ($reader -notmatch [regex]::Escape($required)) {
        throw "Authorization projection is missing managed-policy activation evidence: $required"
    }
}
foreach ($forbidden in @('legacy_grants', 'identity_access.permission_policies', 'identity_access.policy_statements', 'identity_access.group_policy_bindings')) {
    if ($reader -match [regex]::Escape($forbidden)) {
        throw "Legacy tenant-policy authorization fallback must be closed: $forbidden"
    }
}

Write-Host "Managed policy publication and authorization projection validation passed."
