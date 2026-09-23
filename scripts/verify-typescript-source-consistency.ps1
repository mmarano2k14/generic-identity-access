[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "clients/typescript/src"
$required = @(
    "client.ts",
    "authorization-context.ts",
    "admin-ui-builder.ts",
    "require-capability.ts",
    "contracts.ts",
    "admin-contracts.ts",
    "errors.ts",
    "index.ts"
)
foreach ($name in $required) {
    if (-not (Test-Path (Join-Path $src $name) -PathType Leaf)) {
        throw "TypeScript connector source is incomplete: missing $name."
    }
}
$client = Get-Content (Join-Path $src "client.ts") -Raw
$context = Get-Content (Join-Path $src "authorization-context.ts") -Raw
$builder = Get-Content (Join-Path $src "admin-ui-builder.ts") -Raw
$decorator = Get-Content (Join-Path $src "require-capability.ts") -Raw
$index = Get-Content (Join-Path $src "index.ts") -Raw
if ($client -notmatch 'export\s+class\s+IdentityAccessClient\b') { throw "IdentityAccessClient must be a class." }
if ($context -notmatch 'export\s+class\s+IdentityAuthorizationContext\b') { throw "IdentityAuthorizationContext must be a class." }
if ($builder -notmatch 'export\s+class\s+IdentityAccessAdminUiBuilder\b') { throw "IdentityAccessAdminUiBuilder must be a class." }
if ($decorator -notmatch 'export\s+function\s+RequireCapability\b') { throw "RequireCapability decorator is missing." }
if ($index -match 'createIdentityAccessClient') { throw "Legacy createIdentityAccessClient export must not return." }
$allSource = (Get-ChildItem $src -Filter *.ts -Recurse | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
if ($allSource -match 'export\s+function\s+createIdentityAccessClient\b') { throw "Legacy functional client factory must not return." }
if ($context -notmatch '\bisAllowed\s*\(') { throw "IdentityAuthorizationContext.isAllowed is required." }
$requiredClientMethods = @(
    "passwordLogin",
    "validateSession",
    "logout",
    "authorizeOidc",
    "exchangeAuthorizationCode",
    "refreshOidcTokens",
    "listUsers",
    "getUser",
    "createUser",
    "updateUser",
    "listTenants",
    "getTenant",
    "createTenant",
    "updateTenant",
    "getTenantMembership",
    "findTenantMembershipByUser",
    "createTenantMembership",
    "updateTenantMembership",
    "listGroups",
    "getGroup",
    "createGroup",
    "updateGroup",
    "listGroupMembers",
    "addGroupMember",
    "removeGroupMember",
    "listPolicies",
    "getPolicy",
    "createPolicy",
    "updatePolicy",
    "listPolicyStatements",
    "addPolicyStatement",
    "removePolicyStatement",
    "listPolicyBindings",
    "addPolicyBinding",
    "removePolicyBinding",
    "listResourceScopes",
    "getResourceScope",
    "createResourceScope",
    "updateResourceScope",
    "listScopeTypes",
    "addScopeType",
    "revokeUserSessions",
    "revokeClientSessions",
    "getScopeAuthorityGroup",
    "createScopeAuthorityGroup",
    "updateScopeAuthorityGroup",
    "listScopeAuthorityMembers",
    "addScopeAuthorityMember",
    "removeScopeAuthorityMember",
    "getScopeAuthorityPolicy",
    "createScopeAuthorityPolicy",
    "updateScopeAuthorityPolicy",
    "listScopeAuthorityPolicyStatements",
    "addScopeAuthorityPolicyStatement",
    "removeScopeAuthorityPolicyStatement",
    "listScopeAuthorityPolicyBindings",
    "addScopeAuthorityPolicyBinding",
    "removeScopeAuthorityPolicyBinding"
)
foreach ($method in $requiredClientMethods) {
    if ($client -notmatch ("\b" + [regex]::Escape($method) + "\s*\(")) {
        throw "IdentityAccessClient.$method is required."
    }
}
if ($client -match 'client_secret') { throw "The TypeScript public-client implementation must not introduce client_secret." }

if ($client -notmatch 'capabilityPatternSegment') { throw "Typed policy administration must preserve supported wildcard capability patterns." }
if ($client -match 'class\s+Identity.*AdministrationClient\b') { throw "Administration must remain on IdentityAccessClient; do not introduce a parallel runtime client class." }
if ($builder -notmatch '\bwithAll\s*\(') { throw "IdentityAccessAdminUiBuilder.withAll is required." }
if ($builder -notmatch '\bwithTenantAuthorization\s*\(') { throw "Tenant-scoped UI visibility must use an explicit tenant authorization context." }
if ($builder -notmatch '\bhref\s*:') { throw "Administration UI entries must carry stable route metadata." }
$nextAdmin = Join-Path $root "examples/nextjs/admin"
$requiredNextFiles = @(
    "server/IdentityAccessAdminRequest.ts",
    "server/IdentityAccessAdminMutationService.ts",
    "contracts/AdminActionState.ts",
    "components/AdminNavigation.tsx",
    "components/AdminEntityTable.tsx",
    "components/AdminMutationDialog.tsx",
    "components/AdminField.tsx",
    "components/AdminEmptyState.tsx",
    "app/identity/actions.ts",
    "app/identity/layout.tsx",
    "app/identity/loading.tsx",
    "app/identity/error.tsx",
    "app/identity/users/page.tsx",
    "app/identity/tenants/page.tsx",
    "app/identity/memberships/page.tsx",
    "app/identity/groups/page.tsx",
    "app/identity/policies/page.tsx",
    "app/identity/resource-scopes/page.tsx",
    "app/identity/sessions/page.tsx",
    "app/identity/authority/page.tsx",
    "styles/identity-access-admin.css"
)
foreach ($relative in $requiredNextFiles) {
    if (-not (Test-Path (Join-Path $nextAdmin $relative) -PathType Leaf)) {
        throw "Next.js administration module is incomplete: missing $relative."
    }
}
$mutationService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminMutationService.ts") -Raw
$actions = Get-Content (Join-Path $nextAdmin "app/identity/actions.ts") -Raw
$layout = Get-Content (Join-Path $nextAdmin "app/identity/layout.tsx") -Raw
$sessionsPage = Get-Content (Join-Path $nextAdmin "app/identity/sessions/page.tsx") -Raw
if ($mutationService -notmatch 'export\s+class\s+IdentityAccessAdminMutationService\b') { throw "Next.js administration mutation orchestration must remain class-based." }
if ($actions -notmatch '^"use server";') { throw "Next.js administration actions must be explicit server actions." }
if ($actions -match '\.client\.') { throw "Next.js Server Action adapters must delegate through IdentityAccessAdminMutationService instead of calling IdentityAccessClient directly." }
if ($mutationService -notmatch 'requireConfirmation') { throw "Security-sensitive administration mutations must retain explicit confirmation validation." }
if ($sessionsPage -notmatch 'confirmation' -or $sessionsPage -notmatch 'dangerous') { throw "Session revocation UI must retain explicit destructive confirmation." }
if ($layout -notmatch 'styles/identity-access-admin\.css') { throw "The administration layout must import the single shared CSS file." }
$cssFiles = @(Get-ChildItem $nextAdmin -Recurse -File -Include *.css)
if ($cssFiles.Count -ne 1) { throw "Next.js administration must own exactly one CSS file; found $($cssFiles.Count)." }
if ($cssFiles[0].Name -ne 'identity-access-admin.css') { throw "The single administration CSS file must be identity-access-admin.css." }
$tsxSource = (Get-ChildItem $nextAdmin -Recurse -File -Include *.tsx | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
if ($tsxSource -match 'style\s*=\s*\{') { throw "Inline React style objects are not allowed in the administration module; use the single shared CSS file." }
if ($tsxSource -match '<style[ >]') { throw "Component-local style blocks are not allowed in the administration module." }
$moduleCss = @(Get-ChildItem $nextAdmin -Recurse -File -Filter *.module.css)
if ($moduleCss.Count -ne 0) { throw "CSS Modules are not allowed in the administration module." }
Write-Host "TypeScript source consistency validation passed."
