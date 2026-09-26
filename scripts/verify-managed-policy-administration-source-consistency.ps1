[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required managed-policy administration source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$contract = Read-Source "src/IdentityAccess.Application/Administration/IManagedPolicyAdministrationService.cs"
if ($contract -match 'TenantId|TenantReference|IdentityTenantAdministrationContext') {
    throw "Managed policy administration contracts must remain tenant-independent."
}
foreach ($required in @('ListPoliciesAsync', 'CreatePolicyAsync', 'CreateVersionAsync', 'PublishVersionAsync', 'AddStatementAsync', 'RemoveStatementAsync')) {
    if ($contract -notmatch [regex]::Escape($required)) {
        throw "Managed policy administration contract is missing operation: $required"
    }
}

$controller = Read-Source "src/IdentityAccess.Api/Controllers/ManagedPoliciesController.cs"
if ($controller -notmatch 'identity-scopes/\{identityScopeId:guid\}/applications/\{applicationKey\}/managed-policies') {
    throw "Managed policy administration must remain identity-scope/application scoped."
}
if ($controller -match 'tenants/\{tenantId') {
    throw "Managed policy administration routes must not acquire tenant ownership."
}
if ($controller -notmatch 'PublishVersion' -or $controller -notmatch 'RemoveStatement') {
    throw "Managed policy administration must preserve explicit publication and draft statement editing."
}

$service = Read-Source "src/IdentityAccess.Application/Administration/ManagedPolicyAdministrationService.cs"
if ($service -match 'TenantReference|tenantId') {
    throw "Managed policy administration service must not use tenant-owned policy identity."
}
if ($service -notmatch 'DatabaseRouteRequest\(application, identityScopeId\)') {
    throw "Managed policy administration must resolve storage from application and identity scope."
}
if ($service -notmatch 'current\.Value\.DefaultVersion') {
    throw "Managed policy metadata updates must preserve default-version selection."
}

$statementStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlManagedPolicyStatementStore.cs"
if ($statementStore -notmatch 'DELETE FROM identity_access\.managed_policy_statements' -or
    $statementStore -notmatch 'pv\.published_at IS NULL') {
    throw "Managed policy statement removal must remain limited to unpublished versions."
}

$client = Read-Source "clients/typescript/src/client/administration/IdentityAccessManagedPoliciesClient.ts"
if ($client -notmatch 'administrationBasePath\(context\).*managed-policies') {
    throw "The public managed-policy client must use the identity-scope/application administration route."
}
if ($client -match 'IdentityTenantAdministrationContext|tenantApplicationPath') {
    throw "The public managed-policy client must not require a tenant context."
}

$registration = Read-Source "src/IdentityAccess.Api/AdministrationServiceRegistration.cs"
if ($registration -notmatch 'IManagedPolicyAdministrationService, ManagedPolicyAdministrationService') {
    throw "Managed policy administration service must remain explicitly registered when its stores are available."
}

Write-Host "Managed policy administration source consistency validation passed."
