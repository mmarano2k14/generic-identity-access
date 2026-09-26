[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required managed-policy-binding administration source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$contract = Read-Source "src/IdentityAccess.Application/Administration/IManagedPolicyBindingAdministrationService.cs"
foreach ($required in @('ListAvailablePoliciesAsync', 'ListBindingsAsync', 'AddBindingAsync', 'RemoveBindingAsync')) {
    if ($contract -notmatch [regex]::Escape($required)) {
        throw "Managed-policy-binding administration contract is missing operation: $required"
    }
}

$service = Read-Source "src/IdentityAccess.Application/Administration/ManagedPolicyBindingAdministrationService.cs"
if ($service -notmatch 'policyVersion \?\? policy\.Value\.DefaultVersion') {
    throw "Managed policy attachment must pin the explicit version or the published default version at binding time."
}
if ($service -notmatch 'ManagedPolicyVersionReference' -or $service -notmatch 'ManagedGroupPolicyBinding\.Restore') {
    throw "Managed policy attachment must persist a concrete managed-policy version reference."
}
if ($service -match 'PermissionPolicyReference') {
    throw "Managed policy binding administration must not reintroduce tenant-owned permission policy identity."
}

$storeContract = Read-Source "src/IdentityAccess.Application/Storage/IManagedPolicyStore.cs"
if ($storeContract -notmatch 'ListAttachableAsync') {
    throw "Managed policy persistence must expose a focused attachable-policy query for tenant binding selection."
}

$store = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedPolicyStore.cs"
if ($store -notmatch 'ListAttachableAsync' -or
    $store -notmatch 'p\.status = @active_policy_status' -or
    $store -notmatch 'p\.default_version IS NOT NULL' -or
    $store -notmatch 'pv\.published_at IS NOT NULL') {
    throw "Attachable managed-policy lookup must filter active policies with a published default version in PostgreSQL before paging."
}
if ($store -notmatch 'LIMIT @limit OFFSET @offset') {
    throw "Attachable managed-policy lookup must remain bounded after server-side filtering."
}

$controller = Read-Source "src/IdentityAccess.Api/Controllers/ManagedPolicyBindingsController.cs"
if ($controller -notmatch 'tenants/\{tenantId:guid\}/applications/\{applicationKey\}/managed-policy-bindings') {
    throw "Managed policy binding administration must remain tenant-scoped."
}
if ($controller -notmatch 'available-policies' -or $controller -notmatch 'PolicyBindings') {
    throw "Managed policy binding administration must expose tenant-authorized shared-policy discovery under policy-binding capability checks."
}

$client = Read-Source "clients/typescript/src/client/administration/IdentityAccessManagedPolicyBindingsClient.ts"
if ($client -notmatch 'IdentityTenantAdministrationContext' -or $client -notmatch 'tenantApplicationPath\(context\)') {
    throw "The managed policy binding client must remain tenant-context scoped."
}
if ($client -match 'administrationBasePath\(context\).*managed-policy-bindings') {
    throw "Managed policy bindings must not become identity-scope-owned resources."
}

$search = Read-Source "examples/nextjs/admin/server/IdentityAccessAdminEntityReferenceSearchService.ts"
if ($search -notmatch 'case "managed-policy"' -or $search -notmatch 'managedPolicyBindings\.listAvailablePolicies') {
    throw "Group policy selection must search tenant-authorized shared managed policies instead of legacy tenant policies."
}

$groupsPage = Read-Source "examples/nextjs/admin/app/identity/groups/page.tsx"
if ($groupsPage -notmatch 'name="managedPolicyId"' -or $groupsPage -notmatch 'kind="managed-policy"') {
    throw "Group binding creation must select from the shared managed-policy catalog."
}
if ($groupsPage -match 'action=\{addGroupPolicyBindingAction\}') {
    throw "New group policy bindings must not be created through the legacy tenant-policy binding path."
}
if ($groupsPage -match 'Legacy compatibility' -or $groupsPage -match 'removeGroupPolicyBindingAction' -or $groupsPage -match '\.administration\.policies') {
    throw "Group administration must not expose the retired legacy tenant-policy compatibility path."
}

Write-Host "Managed policy binding administration source consistency validation passed."
