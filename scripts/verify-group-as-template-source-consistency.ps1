[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required group-as-template source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

# 0026 is historical/checksummed and must remain present; all simplification is additive in 0027.
$legacyMigration = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0026_group_templates.sql"
$newMigration = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0028_simplify_group_templates.sql"
foreach ($token in @('group_templates', 'origin', 'template_id')) {
    if ($legacyMigration -notmatch [regex]::Escape($token)) {
        throw "Historical migration 0026 no longer matches the applied checksum-era schema: $token"
    }
}
foreach ($token in @('is_template boolean NOT NULL DEFAULT FALSE', 'DROP TABLE IF EXISTS identity_access.group_templates', 'retired_seed_group_instances', 'non-development rows in identity_access.group_templates')) {
    if ($newMigration -notmatch [regex]::Escape($token)) {
        throw "Migration 0028 is missing required group-as-template behavior: $token"
    }
}
if ($newMigration -match '\bCASCADE\b') {
    throw "Migration 0028 must clean retired template data through explicit relationships, not CASCADE."
}

$retiredFiles = @(
    'src/IdentityAccess.Domain/GroupTemplate.cs',
    'src/IdentityAccess.Domain/GroupTemplateReference.cs',
    'src/IdentityAccess.Domain/GroupOrigin.cs',
    'src/IdentityAccess.Application/Administration/IGroupTemplateAdministrationService.cs',
    'src/IdentityAccess.Application/Administration/GroupTemplateAdministrationService.cs',
    'src/IdentityAccess.Application/Storage/IGroupTemplateStore.cs',
    'src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlGroupTemplateStore.cs',
    'src/IdentityAccess.Api/Controllers/GroupTemplatesController.cs',
    'src/IdentityAccess.Api/Controllers/TenantGroupTemplatesController.cs',
    'clients/typescript/src/client/administration/IdentityAccessGroupTemplatesClient.ts',
    'scripts/authentication/seed-dev-group-templates.ps1'
)
foreach ($relativePath in $retiredFiles) {
    if (Test-Path -LiteralPath (Join-Path $root $relativePath)) {
        throw "Retired separate GroupTemplate subsystem file still exists: $relativePath"
    }
}

$userGroup = Read-Source "src/IdentityAccess.Domain/UserGroup.cs"
if ($userGroup -notmatch 'bool IsTemplate' -or $userGroup -match 'GroupOrigin|TemplateId') {
    throw "UserGroup must own the reusable-template marker directly without origin/template provenance."
}

$groupStore = Read-Source "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlUserGroupStore.cs"
foreach ($token in @('is_template', 'ListTemplatesAsync', 'CreateFromTemplateAsync', 'managed_group_policy_bindings')) {
    if ($groupStore -notmatch [regex]::Escape($token)) {
        throw "User-group persistence is missing reusable-group behavior: $token"
    }
}
if ($groupStore -match 'INSERT INTO identity_access\.group_memberships') {
    throw "Create-from-template persistence must never clone group memberships."
}

$groupsController = Read-Source "src/IdentityAccess.Api/Controllers/GroupsController.cs"
foreach ($token in @('HttpGet("templates")', 'HttpPost("from-template")', 'reusable-groups', 'UpdateReusableGroupAsync', 'AuthorizeGrantCopyAsync')) {
    if ($groupsController -notmatch [regex]::Escape($token)) {
        throw "GroupsController is missing unified group/template behavior: $token"
    }
}

$delegationGuard = Read-Source "src/IdentityAccess.Api/Security/TenantGroupAssignmentDelegationGuard.cs"
foreach ($token in @('AuthorizeGrantCopyAsync', 'sourceTenantId', 'targetTenantId', 'new ResourceScopeReference(targetTenant')) {
    if ($delegationGuard -notmatch [regex]::Escape($token)) {
        throw "Create-from-template delegation does not preserve source grants and target-tenant authority: $token"
    }
}

$bindingController = Read-Source "src/IdentityAccess.Api/Controllers/ManagedPolicyBindingsController.cs"
if ($bindingController -notmatch 'IGroupDefinitionMutationGuard' -or $bindingController -notmatch 'AuthorizeDefinitionMutationAsync') {
    throw "Managed policy changes on reusable groups must pass the scope-authority definition guard."
}

$client = Read-Source "clients/typescript/src/client/administration/IdentityAccessGroupsClient.ts"
foreach ($token in @('listTemplates', 'createFromTemplate', 'updateReusable')) {
    if ($client -notmatch [regex]::Escape($token)) {
        throw "TypeScript group client is missing unified reusable-group operation: $token"
    }
}
$contracts = Read-Source "clients/typescript/src/admin-contracts.ts"
if ($contracts -notmatch 'readonly isTemplate: boolean' -or $contracts -match 'IdentityGroupOrigin|IdentityGroupTemplateRecord') {
    throw "TypeScript contracts must expose isTemplate on IdentityGroupRecord without a parallel template contract."
}

$page = Read-Source "examples/nextjs/admin/app/identity/groups/page.tsx"
$catalog = Read-Source "examples/nextjs/admin/components/AdminGroupCatalog.tsx"
foreach ($token in @('Create from template', 'Make available as template', 'isTemplate')) {
    if ($page -notmatch [regex]::Escape($token) -and $catalog -notmatch [regex]::Escape($token)) {
        throw "Groups UI is missing unified reusable-group behavior: $token"
    }
}
if ($page -match 'Available group templates|groupTemplates\.' -or $catalog -match 'Available group templates') {
    throw "The retired separate group-template catalogue is still present in the administration UI."
}

Write-Host "Group-as-template source consistency validation passed."

