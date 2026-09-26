[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required managed-policy binding source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$binding = Read-Source "src/IdentityAccess.Domain/ManagedGroupPolicyBinding.cs"
if ($binding -notmatch 'ManagedPolicyVersionReference' -or $binding -notmatch 'GroupReference') {
    throw "Managed group-policy bindings must target one concrete managed-policy version from one tenant-scoped group."
}
if ($binding -match 'PermissionPolicyReference') {
    throw "Managed group-policy bindings must not fall back to tenant-owned permission-policy references."
}
if ($binding -notmatch 'targetScope\.Tenant != group\.Tenant') {
    throw "Managed policy resource targets must remain constrained to the binding group tenant."
}

$migration = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0024_managed_policy_bindings.sql"
if ($migration -notmatch 'managed_group_policy_bindings') {
    throw "Managed-policy binding migration must create the dedicated managed binding table."
}
if ($migration -notmatch 'tenant_id uuid NOT NULL') {
    throw "Managed-policy bindings must retain an explicit tenant boundary."
}
if ($migration -notmatch 'REFERENCES identity_access\.managed_policy_versions') {
    throw "Managed-policy bindings must reference the shared managed-policy version catalog."
}
if ($migration -match 'FOREIGN KEY\s*\(identity_scope_id, tenant_id, application_key, policy_id, policy_version\)\s*REFERENCES identity_access\.managed_policy_versions') {
    throw "Managed policy ownership must not acquire a tenant dimension through the binding foreign key."
}
if ($migration -notmatch 'REFERENCES identity_access\.user_groups' -or
    $migration -notmatch 'REFERENCES identity_access\.resource_scopes') {
    throw "Managed-policy bindings must preserve tenant-scoped group and resource-scope foreign keys."
}

$mutationStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedGroupPolicyBindingMutationStore.cs"
if ($mutationStore -notmatch 'managed_policies' -or $mutationStore -notmatch 'managed_policy_versions') {
    throw "Managed-policy binding mutations must validate current managed policy and version state."
}
if ($mutationStore -match 'permission_policies') {
    throw "Managed-policy binding mutations must not validate against tenant-owned legacy policies."
}
if ($mutationStore -notmatch 'rs\.tenant_id = g\.tenant_id') {
    throw "Managed-policy binding mutations must validate resource targets inside the group tenant."
}
if ($mutationStore -notmatch 'pv\.published_at IS NOT NULL') {
    throw "Managed-policy binding mutations must reject unpublished policy versions."
}

Write-Host "Managed policy binding source consistency validation passed."
