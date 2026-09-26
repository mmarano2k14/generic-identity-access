[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Required multi-tenant administration source file is missing: $RelativePath" }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) { throw "Multi-tenant administration source consistency failure in '$RelativePath': missing '$ExpectedText'." }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) { throw "Multi-tenant administration source consistency failure in '$RelativePath': rejected text '$RejectedText' is present." }
}

$required = @(
    "src/IdentityAccess.Application/Administration/IEffectiveAdministrationContextService.cs",
    "src/IdentityAccess.Application/Administration/EffectiveAdministrationContextService.cs",
    "src/IdentityAccess.Api/Controllers/AdministrationContextController.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlTenantMembershipStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlTenantUserReadStore.cs",
    "src/IdentityAccess.Application/Administration/IAdministrationTenantVisibilityService.cs",
    "src/IdentityAccess.Application/Administration/AdministrationTenantVisibilityService.cs",
    "src/IdentityAccess.Application/Administration/ITenantUserAdministrationService.cs",
    "src/IdentityAccess.Application/Administration/TenantUserAdministrationService.cs",
    "src/IdentityAccess.Api/Controllers/TenantUsersController.cs",
    "clients/typescript/src/client/administration/IdentityAccessAdministrationContextClient.ts",
    "clients/typescript/src/client/administration/IdentityAccessTenantUsersClient.ts",
    "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts",
    "examples/nextjs/admin/server/IdentityAccessAdminTenantUserReferencePresentation.ts",
    "examples/nextjs/admin/server/IdentityAccessAdminUserReadService.ts",
    "examples/nextjs/admin/components/AdminTenantContextSelector.tsx",
    "examples/nextjs/admin/components/AdminTenantTargetField.tsx",
    "examples/nextjs/admin/components/AdminCreateGroupDialog.tsx",
    "clients/typescript/src/admin-ui-builder.ts",
    "examples/nextjs/admin/.env.local.example",
    "examples/nextjs/.env.example"
)
foreach ($file in $required) { Require-File $file | Out-Null }

Reject-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" "IDENTITY_ACCESS_TENANT_ID"
Reject-Text "examples/nextjs/admin/.env.local.example" "IDENTITY_ACCESS_TENANT_ID"
Reject-Text "examples/nextjs/.env.example" "IDENTITY_ACCESS_TENANT_ID"
Reject-Text "scripts/authentication/bootstrap-dev-admin.ps1" "IDENTITY_ACCESS_TENANT_ID="
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" ".administration.context.get(administrationContext)"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" 'tenantVisibility === "membership-limited"'
Require-Text "src/IdentityAccess.Application/Administration/EffectiveAdministrationContextService.cs" "IIdentityScopeAssignedCapabilityReader"
Require-Text "src/IdentityAccess.Application/Administration/EffectiveAdministrationContextService.cs" "ListForSubjectAsync"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlTenantMembershipStore.cs" "AND user_id = @user_id"
Require-Text "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" "if (scopeResult.Decision == IdentityAuthorizationDecision.Allowed)"
Require-Text "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" "return AdministrationAccessResult.Allow();"

Require-Text "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" "HasActiveMembershipAsync"
Require-Text "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" "TenantContextOutsideVisibility"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlTenantUserReadStore.cs" "INNER JOIN identity_access.users AS users"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlTenantUserReadStore.cs" "memberships.tenant_id = @tenant_id"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlTenantUserReadStore.cs" "OFFSET @offset"
Require-Text "src/IdentityAccess.Application/Administration/TenantUserAdministrationService.cs" "ITenantUserReadStore"
Require-Text "src/IdentityAccess.Api/AdministrationServiceRegistration.cs" "AddSingleton<ITenantUserAdministrationService, TenantUserAdministrationService>()"
Require-Text "src/IdentityAccess.Api/ApiFeatureRegistration.cs" "Register<ITenantUserAdministrationService>(builder.Services)"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminEntityReferenceSearchService.ts" ".administration.tenantUsers.list"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminEntityReferenceSearchService.ts" "IdentityAccessAdminTenantUserReferencePresentation.tenantUsers"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminTenantUserReferencePresentation.ts" "export class IdentityAccessAdminTenantUserReferencePresentation"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminTenantUserReferencePresentation.ts" "IdentityTenantUserRecord"
Reject-Text "examples/nextjs/admin/server/IdentityAccessAdminEntityReferenceSearchService.ts" "this.#request.client.administration.memberships.list(tenantContext, options)"

Require-Text "examples/nextjs/admin/app/identity/groups/page.tsx" ".administration.tenantUsers.list"
Reject-Text "examples/nextjs/admin/app/identity/groups/page.tsx" "administration.users.get(request.administrationContext"


Require-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" "selectedTenantContext(requestedTenantId?: string)"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" 'new IdentityAccessClientError("forbidden", 403, "tenant_context_outside_visibility")'
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" 'effectiveContext.tenantVisibility === "scope-wide"'
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" 'kind="tenant"'
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" "activeTenantMemberships"

Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" "Identity Scope Administrator"
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" "Tenant-scoped subject"
Require-Text "examples/nextjs/admin/components/AdminTenantTargetField.tsx" 'effectiveContext.tenantVisibility === "scope-wide"'
Require-Text "examples/nextjs/admin/components/AdminTenantTargetField.tsx" 'kind="tenant"'
Require-Text "examples/nextjs/admin/components/AdminTenantTargetField.tsx" 'name="tenantId"'
Require-Text "examples/nextjs/admin/components/AdminTenantTargetField.tsx" "activeTenantMemberships"
Require-Text "examples/nextjs/admin/components/AdminCreateGroupDialog.tsx" "AdminTenantTargetField"
Require-Text "examples/nextjs/admin/app/identity/groups/page.tsx" "AdminCreateGroupDialog"
Require-Text "examples/nextjs/admin/app/identity/groups/page.tsx" '{ label: "Tenant", value: context.tenantId }'
Require-Text "clients/typescript/src/admin-ui-builder.ts" 'return this.#with("users", "user", "Users", "Manage identities and account lifecycle.", true);'
Require-Text "clients/typescript/src/admin-ui-builder.ts" "withTenantAuthorizations"
Require-Text "clients/typescript/src/admin-ui-builder.ts" "this.#tenantAuthorizations.length > 0"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" "builder.withTenantAuthorizations(tenantAuthorizations)"

foreach ($page in @(
    "examples/nextjs/admin/app/identity/groups/page.tsx",
    "examples/nextjs/admin/app/identity/resource-scopes/page.tsx"
)) {
    Require-Text $page "selectedTenantContext(tenantId)"
    Require-Text $page "AdminTenantContextSelector"
    Require-Text $page 'name="tenantId" value={context.tenantId}'
}

# Managed policy definitions are identity-scope/application resources. They must not
# inherit tenant selection requirements from tenant-owned administration workspaces.
Reject-Text "examples/nextjs/admin/app/identity/policies/page.tsx" "selectedTenantContext(tenantId)"
Reject-Text "examples/nextjs/admin/app/identity/policies/page.tsx" "AdminTenantContextSelector"
Reject-Text "examples/nextjs/admin/app/identity/policies/page.tsx" 'tenantView === "all"'
Reject-Text "examples/nextjs/admin/app/identity/policies/page.tsx" 'name="tenantId"'

Require-Text "examples/nextjs/admin/server/IdentityAccessAdminMutationService.ts" "return this.#request.tenantContextFor(tenantId);"
Reject-Text "examples/nextjs/admin/server/IdentityAccessAdminMutationService.ts" "this.#request.tenantContext()"
Reject-Text "examples/nextjs/admin/server/IdentityAccessAdminAccessInsightService.ts" "this.#request.tenantContext()"

Require-Text "examples/nextjs/admin/server/IdentityAccessAdminUserReadService.ts" 'tenantVisibility === "scope-wide"'
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminUserReadService.ts" ".administration.tenantUsers.list(context"
Require-Text "examples/nextjs/admin/app/identity/users/page.tsx" "IdentityAccessAdminUserReadService"
Require-Text "examples/nextjs/admin/app/identity/users/page.tsx" "AdminTenantContextSelector"
Require-Text "examples/nextjs/admin/app/identity/page.tsx" ".administration.tenantUsers.list(context"
Require-Text "examples/nextjs/admin/app/identity/page.tsx" "AdminTenantContextSelector"


Require-File "examples/nextjs/admin/server/IdentityAccessAdminAuthorizedTenantService.ts" | Out-Null
Require-File "examples/nextjs/admin/server/IdentityAccessAdminTenantAggregateLoader.ts" | Out-Null
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminAuthorizedTenantService.ts" 'tenantVisibility === "membership-limited"'
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminAuthorizedTenantService.ts" ".administration.tenants.list"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminAuthorizedTenantService.ts" "activeTenantMemberships"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminTenantAggregateLoader.ts" 'error.code === "forbidden"'
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminTenantAggregateLoader.ts" "#concurrency = 8"
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" "All authorized tenants"
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" "tenantView=all"
Require-Text "examples/nextjs/admin/components/AdminEntityTable.tsx" "AdminEntityTableTenant"
Require-Text "examples/nextjs/admin/components/AdminEntityTable.tsx" 'data-label="Tenant"'

foreach ($page in @(
    "examples/nextjs/admin/app/identity/page.tsx",
    "examples/nextjs/admin/app/identity/users/page.tsx",
    "examples/nextjs/admin/app/identity/groups/page.tsx",
    "examples/nextjs/admin/app/identity/resource-scopes/page.tsx"
)) {
    Require-Text $page 'tenantView === "all"'
    Require-Text $page "IdentityAccessAdminAuthorizedTenantService"
    Require-Text $page "IdentityAccessAdminTenantAggregateLoader"
    Require-Text $page "allTenantsSelected={allTenants}"
}

Reject-Text "examples/nextjs/admin/server/IdentityAccessAdminRequest.ts" 'tenantContextFor("all")'
Reject-Text "examples/nextjs/admin/app/identity/actions.ts" 'tenantView'


Require-File "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx" | Out-Null
Require-Text "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx" "AdminTenantTargetField"
Require-Text "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx" "onTenantChange={setTenantId}"
Require-Text "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx" 'tenantId={tenantId || undefined}'
Require-Text "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx" 'disabled={!tenantId}'
Require-Text "examples/nextjs/admin/components/AdminCreateResourceScopeDialog.tsx" 'key={tenantId || "no-tenant"}'
Require-Text "examples/nextjs/admin/components/AdminEntityAutocomplete.tsx" "onSelectedIdChange"
Require-Text "examples/nextjs/admin/components/AdminTenantTargetField.tsx" "onTenantChange"
Reject-Text "examples/nextjs/admin/components/AdminTenantTargetField.tsx" "defaultChecked={index === 0}"
Require-Text "examples/nextjs/admin/app/identity/resource-scopes/page.tsx" "AdminCreateResourceScopeDialog"
Reject-Text "examples/nextjs/admin/app/identity/resource-scopes/page.tsx" "const create = context ?"
Require-Text "examples/nextjs/admin/components/AdminTenantContextSelector.tsx" "Aggregate mode never becomes a tenant owner."

Write-Host "Multi-tenant administration context source consistency validation passed."
