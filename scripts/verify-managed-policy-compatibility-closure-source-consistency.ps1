[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required compatibility-closure source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

function Require-Text([string]$relativePath, [string]$pattern, [string]$message) {
    if ((Read-Source $relativePath) -notmatch $pattern) { throw $message }
}

function Reject-Text([string]$relativePath, [string]$pattern, [string]$message) {
    if ((Read-Source $relativePath) -match $pattern) { throw $message }
}

$grantPath = "src/IdentityAccess.Application/Authorization/AssignedCapabilityGrant.cs"
Require-Text $grantPath 'ManagedPolicyVersionReference ManagedPolicyVersion' "Assigned capability provenance must require a managed-policy version."
Reject-Text $grantPath 'PermissionPolicyReference' "Assigned capability grants must not accept legacy permission-policy provenance."

$readerPath = "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlAssignedCapabilityReader.cs"
foreach ($required in @(
    'managed_group_policy_bindings',
    'managed_policies',
    'managed_policy_versions',
    'managed_policy_statements',
    'mpv.published_at IS NOT NULL')) {
    Require-Text $readerPath ([regex]::Escape($required)) "Managed-only authorization projection is missing: $required"
}
foreach ($forbidden in @(
    'legacy_grants',
    'identity_access.permission_policies',
    'identity_access.policy_statements',
    'identity_access.group_policy_bindings',
    'policy_kind')) {
    Reject-Text $readerPath ([regex]::Escape($forbidden)) "Legacy authorization fallback is still active in the assigned-capability projection: $forbidden"
}

$postgresRegistration = "src/IdentityAccess.Infrastructure.PostgreSql/PostgreSqlServiceCollectionExtensions.cs"
foreach ($forbidden in @(
    'AddSingleton<IPermissionPolicyStore',
    'AddSingleton<IPolicyStatementStore',
    'AddSingleton<IGroupPolicyBindingStore',
    'AddSingleton<IGroupPolicyBindingMutationStore')) {
    Reject-Text $postgresRegistration ([regex]::Escape($forbidden)) "Legacy tenant-policy persistence must not be registered: $forbidden"
}
foreach ($required in @(
    'AddSingleton<IManagedPolicyStore',
    'AddSingleton<IManagedPolicyVersionStore',
    'AddSingleton<IManagedPolicyStatementStore',
    'AddSingleton<IManagedGroupPolicyBindingStore',
    'AddSingleton<IManagedGroupPolicyBindingMutationStore')) {
    Require-Text $postgresRegistration ([regex]::Escape($required)) "Managed policy persistence registration is missing: $required"
}

Reject-Text "src/IdentityAccess.Api/AdministrationServiceRegistration.cs" 'AddSingleton<IPolicyAdministrationService' "Legacy policy administration service must not be registered."
Reject-Text "src/IdentityAccess.Api/ApiFeatureRegistration.cs" 'Register<IPolicyAdministrationService>' "Legacy policy administration API feature must not be registered."
Require-Text "src/IdentityAccess.Api/Controllers/PoliciesController.cs" '\[NonController\]' "Legacy policies controller must remain non-discoverable."
Require-Text "src/IdentityAccess.Api/Controllers/PolicyBindingsController.cs" '\[NonController\]' "Legacy policy-bindings controller must remain non-discoverable."

$administrationClient = "clients/typescript/src/client/administration/IdentityAccessAdministrationClient.ts"
Reject-Text $administrationClient 'IdentityAccessPoliciesClient' "TypeScript administration composition must not expose the legacy policies client."
Reject-Text $administrationClient 'public\s+readonly\s+policies\s*:' "TypeScript administration composition must not expose administration.policies."
Reject-Text "clients/typescript/src/index.ts" 'IdentityAddGroupPolicyBindingRequest' "Legacy group-policy binding request must not remain a public package export."
Reject-Text "clients/typescript/src/index.ts" 'IdentityGroupPolicyBindingRecord' "Legacy group-policy binding record must not remain a public package export."

$legacyCreatePolicyDialog = "examples/nextjs/admin/components/AdminCreatePolicyDialog.tsx"
Require-Text $legacyCreatePolicyDialog 'export\s+\{\s*\};' "Retired legacy policy creation dialog must remain an inert overlay tombstone."
foreach ($forbidden in @(
    'createPolicyAction',
    'AdminMutationDialog',
    'AdminTenantTargetField'
)) {
    Reject-Text $legacyCreatePolicyDialog ([regex]::Escape($forbidden)) "Retired legacy policy creation dialog became active again: $forbidden"
}

$nextActions = "examples/nextjs/admin/app/identity/actions.ts"
foreach ($forbidden in @(
    'createPolicyAction',
    'updatePolicyAction',
    'addPolicyStatementAction',
    'removePolicyStatementAction',
    'addGroupPolicyBindingAction',
    'removeGroupPolicyBindingAction',
    'IdentityAccessAdminPolicyMutationService')) {
    Reject-Text $nextActions ([regex]::Escape($forbidden)) "Next.js administration still exposes retired tenant-policy action: $forbidden"
}

$groupsPage = "examples/nextjs/admin/app/identity/groups/page.tsx"
Require-Text $groupsPage 'managedPolicyBindings\.list' "Groups administration must read managed policy bindings."
Require-Text $groupsPage 'addManagedGroupPolicyBindingAction' "Groups administration must create managed policy bindings."
Require-Text $groupsPage 'removeManagedGroupPolicyBindingAction' "Groups administration must remove managed policy bindings."
foreach ($forbidden in @('Legacy compatibility', 'legacyBindings', 'removeGroupPolicyBindingAction', '.administration.policies')) {
    Reject-Text $groupsPage ([regex]::Escape($forbidden)) "Groups administration still exposes legacy tenant-policy compatibility: $forbidden"
}

$insight = "examples/nextjs/admin/server/IdentityAccessAdminAccessInsightService.ts"
Require-Text $insight 'managedPolicyBindings\.list' "Access insight must read managed bindings."
Require-Text $insight 'managedPolicies\.getVersion' "Access insight must preserve exact managed-policy version provenance."
Require-Text $insight 'managedPolicies\.listStatements' "Access insight must read statements from the pinned managed-policy version."
Reject-Text $insight '\.administration\.policies' "Access insight must not read legacy tenant policies."

Reject-Text "examples/nextjs/admin/contracts/AdminEntityReferenceKind.ts" '\| "policy"' "Legacy policy lookup kind must not remain active."
Reject-Text "examples/nextjs/admin/server/IdentityAccessAdminEntityReferenceSearchService.ts" 'case "policy"' "Legacy policy entity lookup must not remain active."

$bootstrap = "scripts/authentication/bootstrap-dev-admin.ps1"
foreach ($required in @(
    'identity_access.managed_policies',
    'identity_access.managed_policy_versions',
    'identity_access.managed_policy_statements',
    'identity_access.managed_group_policy_bindings')) {
    Require-Text $bootstrap ([regex]::Escape($required)) "Development bootstrap must provision tenant authorization through managed policies: $required"
}
foreach ($forbidden in @(
    'INSERT INTO identity_access.permission_policies',
    'INSERT INTO identity_access.policy_statements',
    'INSERT INTO identity_access.group_policy_bindings')) {
    Reject-Text $bootstrap ([regex]::Escape($forbidden)) "Development bootstrap still provisions retired tenant-policy state: $forbidden"
}

$qualification = "scripts/verify-production-qualification.ps1"
Reject-Text $qualification 'verify-permission-persistence\.ps1' "Production qualification must not depend on retired tenant-policy persistence."
Reject-Text $qualification 'verify-wildcard-policy-patterns\.ps1' "Production qualification must not depend on retired tenant-policy wildcard persistence."

Require-Text "scripts/postgresql/verify-resource-scope-hierarchy.ps1" 'managed_group_policy_bindings' "Resource-scope qualification must exercise managed bindings."
Reject-Text "scripts/postgresql/verify-resource-scope-hierarchy.ps1" 'identity_access\.group_policy_bindings' "Resource-scope qualification must not exercise retired tenant-policy bindings."
Require-Text "scripts/postgresql/validate-rbac-capability-alignment.sql" 'managed_policy_statements' "RBAC capability alignment must validate the active managed-policy statement schema."

# Historical schema remains immutable. Closing compatibility is an application/runtime change, not history rewriting.
foreach ($historical in @(
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0003_permission_policy_foundation.sql",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0004_assignment_projection_indexes.sql")) {
    if (-not (Test-Path (Join-Path $root $historical) -PathType Leaf)) {
        throw "Historical legacy migration must remain present: $historical"
    }
}

# Scope authority is a separate identity-scope administration model and must survive tenant-policy retirement.
Require-Text "clients/typescript/src/client/administration/IdentityAccessAdministrationClient.ts" 'scopeAuthority' "Scope-authority composition must remain available."
Require-Text "src/IdentityAccess.Api/AdministrationServiceRegistration.cs" 'IIdentityScopeAuthorityAdministrationService' "Scope-authority administration registration must remain available."

Write-Host "Managed policy legacy compatibility closure source consistency validation passed."
