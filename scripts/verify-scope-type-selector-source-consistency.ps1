[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required scope-type selector source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$migration = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0008_resource_scope_hierarchy.sql"
foreach ($required in @(
    'application_scope_types',
    'pk_application_scope_types',
    'fk_resource_scopes_type',
    'scope_type_key',
    'scope_model_version'
)) {
    if ($migration -notmatch [regex]::Escape($required)) {
        throw "Existing resource-scope schema is missing required scope-type invariant: $required"
    }
}

$client = Read-Source "clients/typescript/src/client/administration/IdentityAccessSecurityModelsClient.ts"
foreach ($required in @('listScopeTypes', 'addScopeType', '/scope-types')) {
    if ($client -notmatch [regex]::Escape($required)) {
        throw "TypeScript security-model client is missing required scope-type contract: $required"
    }
}

$route = Read-Source "examples/nextjs/admin/app/api/identity/scope-types/route.ts"
foreach ($required in @('listScopeTypes', 'administrationContext', 'modelVersion')) {
    if ($route -notmatch [regex]::Escape($required)) {
        throw "Protected scope-type lookup route is missing required behavior: $required"
    }
}

$selector = Read-Source "examples/nextjs/admin/components/AdminResourceScopeTypeFields.tsx"
foreach ($required in @('/api/identity/scope-types', 'AdminSelectField', 'name="scopeType"', 'name="modelVersion"')) {
    if ($selector -notmatch [regex]::Escape($required)) {
        throw "Resource-scope selector component is missing required behavior: $required"
    }
}

$create = Read-Source "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx"
$page = Read-Source "examples/nextjs/admin/app/identity/resource-scopes/page.tsx"
if ($create -notmatch 'AdminResourceScopeTypeFields') {
    throw "Resource-scope creation must use the registered scope-type selector."
}
if ($page -notmatch 'AdminResourceScopeTypeFields') {
    throw "Resource-scope editing must use the registered scope-type selector."
}
if ($create -match '<AdminField\s+label=\"Scope type\"' -or $page -match '<AdminField\s+label=\"Scope type\"') {
    throw "Free-text Scope type administration is still present."
}

$securityModels = Read-Source "examples/nextjs/admin/app/identity/security-models/page.tsx"
$securityActions = Read-Source "examples/nextjs/admin/app/identity/security-models/actions.ts"
$securityMutation = Read-Source "examples/nextjs/admin/server/IdentityAccessAdminSecurityModelMutationService.ts"
foreach ($required in @('Registered scope types', 'addScopeTypeAction', 'Parent scope type')) {
    if ($securityModels -notmatch [regex]::Escape($required)) {
        throw "Security-model UI is missing scope-type catalogue behavior: $required"
    }
}
if ($securityActions -notmatch 'addScopeType') {
    throw "Scope-type server action is not wired to the security-model mutation service."
}
if ($securityMutation -notmatch 'securityModels.addScopeType') {
    throw "Scope-type mutation must use the existing typed client contract."
}

if ($securityModels -match [regex]::Escape('name="modelVersion"')) {
    throw 'Security-model scope-type administration must bind the selected immutable model version server-side instead of posting modelVersion as browser-authored form data.'
}
if ($securityModels -notmatch 'addScopeTypeAction\.bind\(null,\s*selected\.modelVersion\)') {
    throw 'Scope-type registration must bind the selected immutable security-model version into the Server Action.'
}
if ($securityActions -notmatch 'addScopeTypeAction\(\s*modelVersion:\s*number') {
    throw 'Scope-type Server Action must receive the selected model version as a bound server argument.'
}
if ($securityMutation -notmatch 'addScopeType\(modelVersion:\s*number,\s*formData:\s*FormData\)') {
    throw 'Scope-type mutation service must receive modelVersion separately from browser FormData.'
}

Write-Host "Scope-type catalogue and Resource Scope selector source consistency validation passed."
