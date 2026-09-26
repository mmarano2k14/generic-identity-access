[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required managed-policy UI source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$page = Read-Source "examples/nextjs/admin/app/identity/policies/page.tsx"
$readService = Read-Source "examples/nextjs/admin/server/IdentityAccessAdminManagedPolicyReadService.ts"
$mutationService = Read-Source "examples/nextjs/admin/server/IdentityAccessAdminManagedPolicyMutationService.ts"
$createDialog = Read-Source "examples/nextjs/admin/components/AdminCreateManagedPolicyDialog.tsx"
$actions = Read-Source "examples/nextjs/admin/app/identity/actions.ts"
$uiBuilder = Read-Source "clients/typescript/src/admin-ui-builder.ts"
$overview = Read-Source "examples/nextjs/admin/app/identity/page.tsx"

if ($page -match 'AdminTenantContextSelector|tenantView|selectedTenantContext|tenantId') {
    throw "Managed policy catalog administration must not acquire tenant selection or tenant ownership in the Policies workspace."
}
if ($page -match '\.administration\.policies\.' -or $readService -match '\.administration\.policies\.' -or $mutationService -match '\.administration\.policies\.') {
    throw "Managed policy UI must not create or mutate legacy tenant-owned permission policies."
}
if ($readService -notmatch '\.administration\.managedPolicies\.list\s*\(' -or
    $readService -notmatch '\.administration\.managedPolicies\.listVersions\s*\(' -or
    $readService -notmatch '\.administration\.managedPolicies\.listStatements\s*\(' -or
    $readService -notmatch 'IdentityAccessAdminPolicyBuilderService') {
    throw "Managed policy UI reads must remain in a focused server-only service over the managed-policy client and registered capability catalog."
}
if ($mutationService -notmatch '\.administration\.managedPolicies\.(?:create|update|createVersion|publishVersion|addStatement|removeStatement)\s*\(') {
    throw "Managed policy UI mutations must use the tenant-independent managedPolicies client surface."
}
if ($mutationService -match 'tenantContextFor|IdentityTenantAdministrationContext|tenantId') {
    throw "Managed policy UI mutations must remain identity-scope/application scoped and tenant-independent."
}
if ($createDialog -notmatch 'name="policyKey"' -or $createDialog -match 'AdminTenantTargetField|tenantId') {
    throw "Managed policy creation must require a stable policy key and must not choose a tenant owner."
}
foreach ($requiredAction in @(
    'createManagedPolicyAction',
    'updateManagedPolicyAction',
    'createManagedPolicyVersionAction',
    'publishManagedPolicyVersionAction',
    'addManagedPolicyStatementAction',
    'removeManagedPolicyStatementAction'
)) {
    if ($page -notmatch [regex]::Escape($requiredAction) -and $requiredAction -ne 'createManagedPolicyAction') {
        throw "Managed policy workspace is missing action: $requiredAction"
    }
    if ($actions -notmatch [regex]::Escape($requiredAction)) {
        throw "Managed policy Server Actions are missing: $requiredAction"
    }
}
if ($createDialog -notmatch 'createManagedPolicyAction') {
    throw "Managed policy creation dialog must use the managed-policy Server Action."
}
if ($page -notmatch 'name="capability"' -or $page -match 'name="resource"|name="feature"|name="action"') {
    throw "Managed policy statements must be selected from the registered capability catalog, not authored as free-text coordinates."
}
if ($page -notmatch 'publishedAt' -or $page -notmatch 'Make this the default version' -or $page -notmatch 'Published versions are immutable') {
    throw "Managed policy UI must expose draft/published lifecycle, default-version selection, and published immutability."
}
if ($uiBuilder -notmatch 'withPolicies\(\).*?"Managed policies".*?false' -and
    $uiBuilder -notmatch 'return this\.#with\("policies", "policy", "Managed policies", "Manage shared versioned policy definitions and capability statements\.", false\);') {
    throw "Managed policy navigation must use identity-scope authorization rather than tenant authorization."
}
if ($overview -notmatch 'href="/identity/policies"' -or $overview -match '/identity/policies\$\{tenantQuery\}') {
    throw "Overview navigation to managed policies must not carry a tenant context."
}

Write-Host "Managed policy UI source consistency validation passed."
