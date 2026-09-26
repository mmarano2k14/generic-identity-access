[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required managed-policy source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$reference = Read-Source "src/IdentityAccess.Domain/ManagedPolicyReference.cs"
if ($reference -notmatch 'IdentityScopeId' -or $reference -notmatch 'ApplicationKey' -or $reference -notmatch 'PolicyId') {
    throw "Managed policy references must remain identity-scope/application scoped."
}
if ($reference -match 'TenantReference|TenantId') {
    throw "Managed policy definitions must not acquire tenant ownership."
}

$policy = Read-Source "src/IdentityAccess.Domain/ManagedPolicy.cs"
if ($policy -notmatch 'ManagedPolicyKey' -or $policy -notmatch 'DefaultVersion') {
    throw "Managed policies must retain stable catalog keys and explicit default-version selection."
}

$version = Read-Source "src/IdentityAccess.Domain/ManagedPolicyVersion.cs"
if ($version -notmatch 'ApplicationSecurityModelReference') {
    throw "Managed policy versions must remain pinned to one application security-model version."
}

$migration = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0023_managed_policy_catalog.sql"
foreach ($table in @('managed_policies', 'managed_policy_versions', 'managed_policy_statements')) {
    if ($migration -notmatch [regex]::Escape($table)) {
        throw "Managed policy migration is missing required table: $table"
    }
}
if ($migration -match 'tenant_id') {
    throw "Managed policy catalog tables must not contain tenant ownership."
}
if ($migration -notmatch 'default_version' -or $migration -notmatch 'model_version') {
    throw "Managed policy catalog migration must retain policy-version and security-model pinning."
}

$metadataStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedPolicyStore.cs"
$versionStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedPolicyVersionStore.cs"
$statementStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedPolicyStatementStore.cs"
foreach ($source in @($metadataStore, $versionStore, $statementStore)) {
    if ($source -match 'tenant_id|TenantReference') {
        throw "Managed policy persistence must remain tenant-independent."
    }
}

Write-Host "Managed policy catalog source consistency validation passed."
