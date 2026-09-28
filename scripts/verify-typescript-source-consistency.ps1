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
    "index.ts",
    "client/IdentityAccessClient.ts",
    "client/IdentityAccessHttpTransport.ts",
    "client/IdentityAccessSystemClient.ts",
    "client/IdentityAccessAuthenticationClient.ts",
    "client/IdentityAccessOidcClient.ts",
    "client/IdentityAccessAuthorizationClient.ts",
    "client/IdentityAccessValueCodec.ts",
    "client/IdentityAccessPathBuilder.ts",
    "client/IdentityAccessProtocolCodec.ts",
    "client/IdentityAccessAdministrationCodec.ts",
    "client/IdentityAccessCrypto.ts",
    "client/administration/IdentityAccessAdministrationClient.ts",
    "client/administration/IdentityAccessAdministrationContextClient.ts",
    "client/administration/IdentityAccessAdministrationTransport.ts",
    "client/administration/IdentityAccessUsersClient.ts",
    "client/administration/IdentityAccessTenantsClient.ts",
    "client/administration/IdentityAccessTenantUsersClient.ts",
    "client/administration/IdentityAccessMembershipsClient.ts",
    "client/administration/IdentityAccessMembershipCandidatesClient.ts",
    "client/administration/IdentityAccessTenantGroupAssignmentsClient.ts",
    "client/administration/IdentityAccessMfaClient.ts",
    "client/administration/IdentityAccessManagedPoliciesClient.ts",
    "client/administration/IdentityAccessManagedPolicyBindingsClient.ts",
    "client/administration/IdentityAccessGroupsClient.ts",
    "client/administration/IdentityAccessResourceScopesClient.ts",
    "client/administration/IdentityAccessSecurityModelsClient.ts",
    "client/administration/IdentityAccessSessionsClient.ts",
    "client/administration/IdentityAccessScopeAuthorityClient.ts",
    "client/administration/IdentityAccessSecurityAuditClient.ts"
)
foreach ($name in $required) {
    if (-not (Test-Path (Join-Path $src $name) -PathType Leaf)) {
        throw "TypeScript connector source is incomplete: missing $name."
    }
}

$compatClient = Get-Content (Join-Path $src "client.ts") -Raw
$client = Get-Content (Join-Path $src "client/IdentityAccessClient.ts") -Raw
$systemClient = Get-Content (Join-Path $src "client/IdentityAccessSystemClient.ts") -Raw
$authenticationClient = Get-Content (Join-Path $src "client/IdentityAccessAuthenticationClient.ts") -Raw
$oidcClient = Get-Content (Join-Path $src "client/IdentityAccessOidcClient.ts") -Raw
$authorizationClient = Get-Content (Join-Path $src "client/IdentityAccessAuthorizationClient.ts") -Raw
$administrationClient = Get-Content (Join-Path $src "client/administration/IdentityAccessAdministrationClient.ts") -Raw
$administrationContextClient = Get-Content (Join-Path $src "client/administration/IdentityAccessAdministrationContextClient.ts") -Raw
$usersClient = Get-Content (Join-Path $src "client/administration/IdentityAccessUsersClient.ts") -Raw
$tenantsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessTenantsClient.ts") -Raw
$tenantUsersClient = Get-Content (Join-Path $src "client/administration/IdentityAccessTenantUsersClient.ts") -Raw
$membershipsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessMembershipsClient.ts") -Raw
$membershipCandidatesClient = Get-Content (Join-Path $src "client/administration/IdentityAccessMembershipCandidatesClient.ts") -Raw
$tenantGroupAssignmentsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessTenantGroupAssignmentsClient.ts") -Raw
$mfaClient = Get-Content (Join-Path $src "client/administration/IdentityAccessMfaClient.ts") -Raw
$managedPoliciesClient = Get-Content (Join-Path $src "client/administration/IdentityAccessManagedPoliciesClient.ts") -Raw
$managedPolicyBindingsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessManagedPolicyBindingsClient.ts") -Raw
$groupsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessGroupsClient.ts") -Raw
$resourceScopesClient = Get-Content (Join-Path $src "client/administration/IdentityAccessResourceScopesClient.ts") -Raw
$securityModelsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessSecurityModelsClient.ts") -Raw
$sessionsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessSessionsClient.ts") -Raw
$scopeAuthorityClient = Get-Content (Join-Path $src "client/administration/IdentityAccessScopeAuthorityClient.ts") -Raw
$securityAuditClient = Get-Content (Join-Path $src "client/administration/IdentityAccessSecurityAuditClient.ts") -Raw
$valueCodec = Get-Content (Join-Path $src "client/IdentityAccessValueCodec.ts") -Raw
$context = Get-Content (Join-Path $src "authorization-context.ts") -Raw
$builder = Get-Content (Join-Path $src "admin-ui-builder.ts") -Raw
$decorator = Get-Content (Join-Path $src "require-capability.ts") -Raw
$index = Get-Content (Join-Path $src "index.ts") -Raw
$allSource = (Get-ChildItem $src -Filter *.ts -Recurse | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"

if ($client -notmatch 'export\s+class\s+IdentityAccessClient\b') { throw "IdentityAccessClient must be a class." }
if ($compatClient -match 'export\s+class\s+IdentityAccessClient\b') { throw "client.ts must remain a compatibility re-export; implementation belongs under src/client/." }
if ($context -notmatch 'export\s+class\s+IdentityAuthorizationContext\b') { throw "IdentityAuthorizationContext must be a class." }
if ($builder -notmatch 'export\s+class\s+IdentityAccessAdminUiBuilder\b') { throw "IdentityAccessAdminUiBuilder must be a class." }
if ($decorator -notmatch 'export\s+function\s+RequireCapability\b') { throw "RequireCapability decorator is missing." }
if ($index -match 'createIdentityAccessClient') { throw "Legacy createIdentityAccessClient export must not return." }
if ($allSource -match 'export\s+function\s+createIdentityAccessClient\b') { throw "Legacy functional client factory must not return." }
if ($context -notmatch '\bisAllowed\s*\(') { throw "IdentityAuthorizationContext.isAllowed is required." }

$rootProperties = @("system", "authentication", "oidc", "authorization", "administration")
foreach ($property in $rootProperties) {
    if ($client -notmatch ("public\s+readonly\s+" + [regex]::Escape($property) + "\s*:")) {
        throw "IdentityAccessClient must expose the $property class responsibility."
    }
}
if ($administrationClient -match 'IdentityAccessPoliciesClient' -or $administrationClient -match 'public\s+readonly\s+policies\s*:') {
    throw "IdentityAccessAdministrationClient must not expose the retired legacy tenant-policy client."
}

$legacyRootMethods = @(
    "liveness", "readiness", "info", "passwordLogin", "validateSession", "logout",
    "authorizeOidc", "exchangeAuthorizationCode", "refreshOidcTokens",
    "listUsers", "createUser", "listTenants", "createTenant", "listGroups", "createGroup",
    "listPolicies", "createPolicy", "listResourceScopes", "revokeUserSessions", "evaluateCapability"
)
foreach ($method in $legacyRootMethods) {
    if ($client -match ("\b" + [regex]::Escape($method) + "\s*\(")) {
        throw "IdentityAccessClient must remain a composition facade; $method belongs to a focused client class."
    }
}

$classRequirements = @(
    @{ Source = $administrationContextClient; ClassName = "IdentityAccessAdministrationContextClient"; Methods = @("get") },
    @{ Source = $systemClient; ClassName = "IdentityAccessSystemClient"; Methods = @("liveness", "readiness", "info") },
    @{ Source = $authenticationClient; ClassName = "IdentityAccessAuthenticationClient"; Methods = @("passwordLogin", "validateSession", "logout") },
    @{ Source = $oidcClient; ClassName = "IdentityAccessOidcClient"; Methods = @("authorize", "exchangeAuthorizationCode", "refreshTokens") },
    @{ Source = $authorizationClient; ClassName = "IdentityAccessAuthorizationClient"; Methods = @("validateContext", "evaluate") },
    @{ Source = $usersClient; ClassName = "IdentityAccessUsersClient"; Methods = @("list", "get", "create", "update") },
    @{ Source = $tenantsClient; ClassName = "IdentityAccessTenantsClient"; Methods = @("list", "get", "create", "update") },
    @{ Source = $tenantUsersClient; ClassName = "IdentityAccessTenantUsersClient"; Methods = @("list") },
    @{ Source = $membershipsClient; ClassName = "IdentityAccessMembershipsClient"; Methods = @("list", "get", "findByUser", "create", "update") },
    @{ Source = $membershipCandidatesClient; ClassName = "IdentityAccessMembershipCandidatesClient"; Methods = @("findByLogin", "createMembershipByLogin") },
    @{ Source = $tenantGroupAssignmentsClient; ClassName = "IdentityAccessTenantGroupAssignmentsClient"; Methods = @("list") },
    @{ Source = $mfaClient; ClassName = "IdentityAccessMfaClient"; Methods = @("listProviders", "getPolicy", "createPolicy", "updatePolicy", "listAuthenticators", "getUserSecurityState", "revokeAuthenticator", "revokeAuthenticatorForRecovery") },
    @{ Source = $managedPoliciesClient; ClassName = "IdentityAccessManagedPoliciesClient"; Methods = @("list", "get", "create", "update", "listVersions", "getVersion", "createVersion", "publishVersion", "listStatements", "addStatement", "removeStatement") },
    @{ Source = $managedPolicyBindingsClient; ClassName = "IdentityAccessManagedPolicyBindingsClient"; Methods = @("listAvailablePolicies", "list", "add", "remove") },
    @{ Source = $groupsClient; ClassName = "IdentityAccessGroupsClient"; Methods = @("list", "get", "create", "update", "listMembers", "addMember", "removeMember") },
    @{ Source = $resourceScopesClient; ClassName = "IdentityAccessResourceScopesClient"; Methods = @("list", "get", "create", "update") },
    @{ Source = $securityModelsClient; ClassName = "IdentityAccessSecurityModelsClient"; Methods = @("list", "get", "registerManifest", "listScopeTypes", "addScopeType") },
    @{ Source = $sessionsClient; ClassName = "IdentityAccessSessionsClient"; Methods = @("revokeUser", "revokeClient") },
    @{ Source = $scopeAuthorityClient; ClassName = "IdentityAccessScopeAuthorityClient"; Methods = @("listGroups", "getGroup", "createGroup", "updateGroup", "listMembers", "addMember", "removeMember", "listPolicies", "getPolicy", "createPolicy", "updatePolicy", "listPolicyStatements", "addPolicyStatement", "removePolicyStatement", "listPolicyBindings", "addPolicyBinding", "removePolicyBinding") },
    @{ Source = $securityAuditClient; ClassName = "IdentityAccessSecurityAuditClient"; Methods = @("list") }
)
foreach ($requirement in $classRequirements) {
    $source = [string]$requirement.Source
    $className = [string]$requirement.ClassName
    if ($source -notmatch ("export\s+class\s+" + [regex]::Escape($className) + "\b")) {
        throw "$className must remain a class."
    }
    foreach ($method in $requirement.Methods) {
        if ($source -notmatch ("\b" + [regex]::Escape([string]$method) + "\s*\(")) {
            throw "$className.$method is required."
        }
    }
}

if ($administrationClient -notmatch 'public\s+readonly\s+context\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+users\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+tenantUsers\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+membershipCandidates\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+tenantGroupAssignments\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+mfa\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+managedPolicies\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+managedPolicyBindings\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+scopeAuthority\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+securityAudit\s*:') {
    throw "IdentityAccessAdministrationClient must compose the focused administration clients."
}
if ($allSource -match 'client_secret') { throw "The TypeScript public-client implementation must not introduce client_secret." }
if ($valueCodec -notmatch 'capabilityPatternSegment') { throw "Typed policy administration must preserve supported wildcard capability patterns." }
if ($context -notmatch '\.authorization\.evaluate\s*\(') { throw "IdentityAuthorizationContext must delegate through IdentityAccessAuthorizationClient." }
if ($builder -notmatch '\bwithAll\s*\(') { throw "IdentityAccessAdminUiBuilder.withAll is required." }
if ($builder -notmatch '\bwithSecurityModels\s*\(') { throw "IdentityAccessAdminUiBuilder must expose the application security-model workspace." }
if ($builder -notmatch '\bwithTenantAuthorization\s*\(') { throw "Tenant-scoped UI visibility must use an explicit tenant authorization context." }
if ($builder -notmatch '\bhref\s*:') { throw "Administration UI entries must carry stable route metadata." }
$nextAdmin = Join-Path $root "examples/nextjs/admin"
$requiredNextFiles = @(
    "server/IdentityAccessAdminRequest.ts",
    "server/IdentityAccessAdminMutationService.ts",
    "server/IdentityAccessAdminPolicyBuilderService.ts",
    "server/IdentityAccessAdminManagedPolicyReadService.ts",
    "server/IdentityAccessAdminManagedPolicyMutationService.ts",
    "server/IdentityAccessServerConnector.ts",
    "contracts/AdminActionState.ts",
    "components/AdminEntityAutocomplete.tsx",
    "components/AdminExactMemberLookup.tsx",
    "components/AdminAddTenantMemberDialog.tsx",
    "components/AdminManageMemberGroupsDialog.tsx",
    "server/IdentityAccessAdminMembershipOverviewService.ts",
    "app/api/identity/membership-candidates/route.ts",
    "contracts/AdminEntityReferenceOption.ts",
    "contracts/AdminEntityReferenceKind.ts",
    "server/IdentityAccessAdminEntityReferencePresentation.ts",
    "server/IdentityAccessAdminEntityReferenceSearchService.ts",
    "app/api/identity/entity-references/route.ts",
    "components/AdminNavigation.tsx",
    "components/AdminNavigationActiveLink.tsx",
    "components/AdminMobileNavigation.tsx",
    "components/AdminCurrentSection.tsx",
    "components/AdminIcon.tsx",
    "components/AdminMetricCard.tsx",
    "components/AdminFeatureCard.tsx",
    "components/AdminDetailCard.tsx",
    "components/AdminRecordContext.tsx",
    "components/AdminAccessInsight.tsx",
    "components/AdminSecurityAuditTimeline.tsx",
    "components/AdminSessionSecurityTimeline.tsx",
    "components/AdminSecurityBanner.tsx",
    "components/AdminEntityTable.tsx",
    "components/AdminMutationDialog.tsx",
    "components/AdminCreateManagedPolicyDialog.tsx",
    "components/AdminFailureFeedback.tsx",
    "components/AdminField.tsx",
    "components/AdminEmptyState.tsx",
    "app/identity/actions.ts",
    "app/identity/layout.tsx",
    "app/identity/page.tsx",
    "app/identity/loading.tsx",
    "app/identity/error.tsx",
    "app/identity/users/page.tsx",
    "app/identity/tenants/page.tsx",
    "app/identity/memberships/page.tsx",
    "app/identity/groups/page.tsx",
    "app/identity/policies/page.tsx",
    "app/identity/security-models/page.tsx",
    "app/identity/resource-scopes/page.tsx",
    "app/identity/mfa/page.tsx",
    "app/identity/sessions/page.tsx",
    "app/identity/sessions/actions.ts",
    "app/identity/authority/page.tsx",
    "app/identity/security-audit/page.tsx",
    "app/layout.tsx",
    "app/page.tsx",
    "app/login/page.tsx",
    "app/login/LoginForm.tsx",
    "app/login/actions.ts",
    "app/recovery/page.tsx",
    "app/recovery/RecoveryForm.tsx",
    "app/recovery/actions.ts",
    "app/auth/callback/page.tsx",
    "server/IdentityAccessHostSessionService.ts",
    "server/IdentityAccessAdminAccessInsightService.ts",
    "server/IdentityAccessAdminSecurityAuditQuery.ts",
    "server/IdentityAccessAdminSecurityAuditService.ts",
    "server/IdentityAccessAdminSecurityAuditPresentation.ts",
    "server/IdentityAccessAdminSecurityAuditSummary.ts",
    "server/IdentityAccessAdminSessionQuery.ts",
    "server/IdentityAccessAdminSessionService.ts",
    "server/IdentityAccessAdminSessionPresentation.ts",
    "server/IdentityAccessAdminSessionSummary.ts",
    "server/IdentityAccessAdminSessionMutationService.ts",
    "server/IdentityAccessAdminSecurityManifestParser.ts",
    "server/IdentityAccessAdminSecurityModelMutationService.ts",
    "server/IdentityAccessAdminSecurityModelFailurePresentation.ts",
    "server/IdentityAccessAdminFailurePresentation.ts",
    "contracts/LoginActionState.ts",
    "contracts/RecoveryActionState.ts",
    "styles/identity-access-admin.css",
    "package.json",
    "tsconfig.json",
    "next.config.ts",
    "scripts/build-local-client.mjs",
    ".env.local.example"
)
foreach ($relative in $requiredNextFiles) {
    if (-not (Test-Path (Join-Path $nextAdmin $relative) -PathType Leaf)) {
        throw "Next.js administration module is incomplete: missing $relative."
    }
}
$mutationService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminMutationService.ts") -Raw
$policyBuilderService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminPolicyBuilderService.ts") -Raw
$managedPolicyReadService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminManagedPolicyReadService.ts") -Raw
$managedPolicyMutationService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminManagedPolicyMutationService.ts") -Raw
$securityManifestParser = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityManifestParser.ts") -Raw
$securityModelMutationService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityModelMutationService.ts") -Raw
$securityModelFailurePresentation = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityModelFailurePresentation.ts") -Raw
$failurePresentation = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminFailurePresentation.ts") -Raw
$adminActionState = Get-Content (Join-Path $nextAdmin "contracts/AdminActionState.ts") -Raw
$actions = Get-Content (Join-Path $nextAdmin "app/identity/actions.ts") -Raw
$layout = Get-Content (Join-Path $nextAdmin "app/identity/layout.tsx") -Raw
$sessionsPage = Get-Content (Join-Path $nextAdmin "app/identity/sessions/page.tsx") -Raw
$overviewPage = Get-Content (Join-Path $nextAdmin "app/identity/page.tsx") -Raw
$adminCss = Get-Content (Join-Path $nextAdmin "styles/identity-access-admin.css") -Raw
$detailCard = Get-Content (Join-Path $nextAdmin "components/AdminDetailCard.tsx") -Raw
$recordContext = Get-Content (Join-Path $nextAdmin "components/AdminRecordContext.tsx") -Raw
$accessInsight = Get-Content (Join-Path $nextAdmin "components/AdminAccessInsight.tsx") -Raw
$accessInsightService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminAccessInsightService.ts") -Raw
$securityAuditQuery = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityAuditQuery.ts") -Raw
$securityAuditService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityAuditService.ts") -Raw
$securityAuditPresentation = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityAuditPresentation.ts") -Raw
$securityAuditSummary = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSecurityAuditSummary.ts") -Raw
$securityAuditPage = Get-Content (Join-Path $nextAdmin "app/identity/security-audit/page.tsx") -Raw
$securityAuditTimeline = Get-Content (Join-Path $nextAdmin "components/AdminSecurityAuditTimeline.tsx") -Raw
$sessionQuery = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSessionQuery.ts") -Raw
$sessionService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSessionService.ts") -Raw
$sessionPresentation = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSessionPresentation.ts") -Raw
$sessionSummary = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSessionSummary.ts") -Raw
$sessionMutationService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminSessionMutationService.ts") -Raw
$sessionActions = Get-Content (Join-Path $nextAdmin "app/identity/sessions/actions.ts") -Raw
$sessionTimeline = Get-Content (Join-Path $nextAdmin "components/AdminSessionSecurityTimeline.tsx") -Raw
$failureFeedback = Get-Content (Join-Path $nextAdmin "components/AdminFailureFeedback.tsx") -Raw
$adminField = Get-Content (Join-Path $nextAdmin "components/AdminField.tsx") -Raw
$entityTable = Get-Content (Join-Path $nextAdmin "components/AdminEntityTable.tsx") -Raw
$entityAutocomplete = Get-Content (Join-Path $nextAdmin "components/AdminEntityAutocomplete.tsx") -Raw
$entityReferencePresentation = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminEntityReferencePresentation.ts") -Raw
$entityReferenceSearchService = Get-Content (Join-Path $nextAdmin "server/IdentityAccessAdminEntityReferenceSearchService.ts") -Raw
$entityReferenceRoute = Get-Content (Join-Path $nextAdmin "app/api/identity/entity-references/route.ts") -Raw
$usersPage = Get-Content (Join-Path $nextAdmin "app/identity/users/page.tsx") -Raw
$tenantsPage = Get-Content (Join-Path $nextAdmin "app/identity/tenants/page.tsx") -Raw
$membershipsPage = Get-Content (Join-Path $nextAdmin "app/identity/memberships/page.tsx") -Raw
$groupsPage = Get-Content (Join-Path $nextAdmin "app/identity/groups/page.tsx") -Raw
$policiesPage = Get-Content (Join-Path $nextAdmin "app/identity/policies/page.tsx") -Raw
$securityModelsPage = Get-Content (Join-Path $nextAdmin "app/identity/security-models/page.tsx") -Raw
$resourceScopesPage = Get-Content (Join-Path $nextAdmin "app/identity/resource-scopes/page.tsx") -Raw
$mfaPage = Get-Content (Join-Path $nextAdmin "app/identity/mfa/page.tsx") -Raw
$authorityPage = Get-Content (Join-Path $nextAdmin "app/identity/authority/page.tsx") -Raw
$localClientBuild = Get-Content (Join-Path $nextAdmin "scripts/build-local-client.mjs") -Raw
function Get-NextAdminOwnedFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Extensions
    )

    $excludedDirectories = @(
        "node_modules",
        ".next",
        "dist",
        "coverage",
        ".turbo",
        "out"
    )

    Get-ChildItem $nextAdmin -Recurse -File | Where-Object {
        $relativePath = $_.FullName.Substring($nextAdmin.Length).TrimStart([char[]]@("\", "/"))
        $segments = $relativePath -split "[\\/]"
        $excluded = $false
        foreach ($segment in $segments) {
            if ($excludedDirectories -contains $segment) {
                $excluded = $true
                break
            }
        }

        (-not $excluded) -and ($Extensions -contains $_.Extension.ToLowerInvariant())
    }
}

$nextTypeScriptSource = (Get-NextAdminOwnedFiles -Extensions @(".ts", ".tsx") | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
$legacyHostClientMethods = 'client\.(?:info|liveness|readiness|passwordLogin|validateSession|logout|authorizeOidc|exchangeAuthorizationCode|refreshOidcTokens|listUsers|createUser|listTenants|createTenant|listGroups|createGroup|listPolicies|createPolicy|listResourceScopes|revokeUserSessions|revokeClientSessions)\s*\('
if ($nextTypeScriptSource -match $legacyHostClientMethods) { throw "Next.js administration must consume focused IdentityAccessClient responsibility classes instead of legacy root methods." }
if ($mutationService -notmatch 'export\s+class\s+IdentityAccessAdminMutationService\b') { throw "Next.js administration mutation orchestration must remain class-based." }
if ($policyBuilderService -notmatch 'export\s+class\s+IdentityAccessAdminPolicyBuilderService\b' -or $policyBuilderService -notmatch '\.administration\.securityModels\.list\s*\(' -or $policyBuilderService -notmatch '\.administration\.securityModels\.get\s*\(') { throw "Policy-builder catalog loading must remain a focused server-only class over registered application security models." }
if ($policyBuilderService -match 'authorization\.evaluate' -or $policyBuilderService -match 'isAllowed\s*\(') { throw "Policy-builder catalog loading must not become a second authorization engine." }
if ($managedPolicyReadService -notmatch 'export\s+class\s+IdentityAccessAdminManagedPolicyReadService\b' -or $managedPolicyReadService -notmatch '\.administration\.managedPolicies\.list\s*\(' -or $managedPolicyReadService -notmatch 'IdentityAccessAdminFailurePresentation\.fromRead') { throw "Managed policy administration reads must remain in a focused server-only service with safe failure projection." }
if ($managedPolicyReadService -match 'authorization\.evaluate' -or $managedPolicyReadService -match 'isAllowed\s*\(') { throw "Managed policy read orchestration must not become an RBAC evaluator." }
if ($policiesPage -notmatch 'IdentityAccessAdminManagedPolicyReadService' -or $policiesPage -notmatch 'AdminFailureFeedback') { throw "Managed policy administration must present server-classified protected-read failures instead of collapsing authorization failures into the generic route error boundary." }
if ($managedPolicyMutationService -notmatch 'export\s+class\s+IdentityAccessAdminManagedPolicyMutationService\b' -or $managedPolicyMutationService -notmatch '\.administration\.managedPolicies\.(?:create|update|createVersion|publishVersion|addStatement|removeStatement)\s*\(') { throw "Managed policy mutations must remain isolated in the focused managed-policy mutation service." }
if ($managedPolicyMutationService -match 'tenantContextFor|IdentityTenantAdministrationContext|tenantId') { throw "Managed policy mutations must remain tenant-independent." }
if ($mutationService -match 'public\s+async\s+(?:createManagedPolicy|updateManagedPolicy|createManagedPolicyVersion|publishManagedPolicyVersion|addManagedPolicyStatement|removeManagedPolicyStatement)\s*\(' -or $mutationService -match '\.administration\.managedPolicies\.(?:create|update|createVersion|publishVersion|addStatement|removeStatement)\s*\(') { throw "Managed policy mutations must not collapse into the general administration mutation service." }
if ($actions -notmatch '^"use server";') { throw "Next.js administration actions must be explicit server actions." }
if ($actions -match '\.client\.') { throw "Next.js Server Action adapters must delegate through focused server-side mutation services instead of calling IdentityAccessClient directly." }
if ($mutationService -notmatch 'requireLiteralConfirmation') { throw "Security-sensitive administration mutations must retain explicit confirmation validation." }
if ($mutationService -match 'revokeUserSessions\s*\(' -or $mutationService -match 'revokeClientSessions\s*\(') { throw "Session mutations must remain outside the general administration mutation service." }
if ($sessionQuery -notmatch 'export\s+class\s+IdentityAccessAdminSessionQuery\b') { throw "Session query normalization must remain a focused class." }
if ($sessionService -notmatch 'export\s+class\s+IdentityAccessAdminSessionService\b' -or $sessionService -notmatch '\.securityAudit\.list\s*\(') { throw "Session activity reads must remain a focused class over the existing security-audit client." }
if ($sessionService -match '\.sessions\.revoke' -or $sessionService -match '\.authorization\.evaluate') { throw "Session read orchestration must not own mutations or become an RBAC evaluator." }
if ($sessionPresentation -notmatch 'export\s+class\s+IdentityAccessAdminSessionPresentation\b') { throw "Session presentation mapping must remain a focused class." }
if ($sessionSummary -notmatch 'export\s+class\s+IdentityAccessAdminSessionSummary\b') { throw "Session summary aggregation must remain a focused class." }
if ($sessionMutationService -notmatch 'export\s+class\s+IdentityAccessAdminSessionMutationService\b' -or $sessionMutationService -notmatch '\.sessions\.revokeUser\s*\(' -or $sessionMutationService -notmatch '\.sessions\.revokeClient\s*\(') { throw "Session mutations must remain in the focused session mutation service and reuse existing typed client contracts." }
if ($sessionMutationService -notmatch 'requireConfirmation') { throw "Session mutations must retain explicit server-side confirmation validation." }
if ($sessionActions -notmatch '^"use server";' -or $sessionActions -match '\.client\.' -or $sessionActions -notmatch 'IdentityAccessAdminSessionMutationService') { throw "Session Server Actions must remain thin adapters over the focused mutation service." }
if ($failurePresentation -notmatch 'export\s+class\s+IdentityAccessAdminFailurePresentation\b' -or $failurePresentation -notmatch 'fromMutation\s*\(' -or $failurePresentation -notmatch 'fromRead\s*\(') { throw "Administration failure classification must remain isolated in IdentityAccessAdminFailurePresentation." }
if ($failurePresentation -notmatch 'httpStatus\s*===\s*409' -or $failurePresentation -notmatch 'case\s+"unauthenticated"' -or $failurePresentation -notmatch 'case\s+"forbidden"' -or $failurePresentation -notmatch 'case\s+"unavailable"' -or $failurePresentation -notmatch 'case\s+"timeout"' -or $failurePresentation -notmatch 'case\s+"transport"' -or $failurePresentation -notmatch 'case\s+"protocol"' -or $failurePresentation -notmatch 'case\s+"configuration"') { throw "Administration failure UX must distinguish authentication, authorization, concurrency, availability, transport, protocol, and configuration failures." }
if ($failurePresentation -match '\.client\.' -or $failurePresentation -match 'authorization\.evaluate' -or $failurePresentation -match 'isAllowed\s*\(') { throw "Failure presentation must not perform API I/O or authorization evaluation." }
if ($mutationService -match 'publicErrorMessage' -or $sessionMutationService -match 'publicErrorMessage' -or $mutationService -match 'IdentityAccessClientError' -or $sessionMutationService -match 'IdentityAccessClientError') { throw "Mutation services must not absorb transport/error-presentation classification." }
if ($actions -notmatch 'IdentityAccessAdminFailurePresentation\.fromMutation' -or $sessionActions -notmatch 'IdentityAccessAdminFailurePresentation\.fromMutation') { throw "Server Actions must delegate safe mutation failure projection to the focused failure-presentation class." }
if ($adminActionState -notmatch 'AdminActionFailureKind' -or $adminActionState -notmatch 'AdminActionRecovery' -or $adminActionState -notmatch 'failure\?:\s*AdminActionFailure') { throw "Administration action state must carry structured safe failure metadata." }
if ($failureFeedback -notmatch 'export\s+function\s+AdminFailureFeedback\b' -or $failureFeedback -notmatch 'failure\.recovery\s*===\s*"sign-in"' -or $failureFeedback -notmatch 'failure\.recovery\s*===\s*"reload"') { throw "Administration failure feedback must expose explicit sign-in and reload recovery guidance without owning security decisions." }
if ($sessionService -notmatch 'evidenceState:\s*"unavailable"' -or $sessionService -notmatch 'IdentityAccessAdminFailurePresentation\.fromRead') { throw "Session investigation must degrade safely when audit evidence is technically unavailable." }
if ($sessionsPage -notmatch 'IdentityAccessAdminSessionQuery' -or $sessionsPage -notmatch 'IdentityAccessAdminSessionService' -or $sessionsPage -notmatch 'AdminSessionSecurityTimeline') { throw "Session administration must retain separated query, read-service, and presentation composition." }
if ($sessionsPage -notmatch 'confirmation' -or $sessionsPage -notmatch 'dangerous') { throw "Session revocation UI must retain explicit destructive confirmation." }
if ($sessionsPage -match 'active-session inventory.*current' -and $sessionsPage -notmatch 'does not expose a session-list contract') { throw "Session administration must not infer current active-session state without a real list contract." }
$sessionUiSource = $sessionsPage + "`n" + $sessionTimeline + "`n" + $sessionService + "`n" + $sessionPresentation + "`n" + $sessionSummary
if ($sessionUiSource -match 'sessionToken|accessToken|refreshTokenHash|sessionTokenHash|passwordHash|totpSecret|connectionString|connectionSecretRef') { throw "Session administration must not render or depend on secret credential material." }
if ($nextTypeScriptSource -match 'SecurityEverythingService|SecurityOperationsService|AdministrationManager|IdentityAccessGodClient') { throw "Cross-cutting God-service names are prohibited in the administration module." }
if ($entityTable -notmatch 'actions\?:\s*ReactNode' -or $entityTable -notmatch 'ia-row-actions') { throw "Administration entity tables must expose reusable per-record action surfaces." }
if ($entityTable -notmatch 'aria-live' -or $entityTable -notmatch 'All statuses' -or $entityTable -notmatch 'Newest version' -or $entityTable -notmatch 'data-label="Identifier"') { throw "Administration entity tables must retain accessible filtering, presentation sorting, live result counts, and responsive data labels." }
if ($entityTable -notmatch 'selectedId\?:\s*string' -or $entityTable -notmatch 'ia-table-row-selected' -or $entityTable -notmatch 'ia-selected-badge') { throw "Administration entity tables must retain presentation-only selected-record context." }
if ($recordContext -notmatch 'export\s+function\s+AdminRecordContext\b' -or $recordContext -notmatch 'closeHref' -or $recordContext -notmatch 'ia-record-context-facts') { throw "Administration detail flows must retain the reusable server-renderable record context." }
if ($accessInsight -notmatch 'export\s+function\s+AdminAccessInsight\b' -or $accessInsight -notmatch 'No allow/deny inference' -or $accessInsight -notmatch 'Assignment structure is not an authorization verdict') { throw "Administration access insight must remain descriptive and must not present assignment structure as an RBAC verdict." }
if ($accessInsightService -notmatch 'export\s+class\s+IdentityAccessAdminAccessInsightService\b' -or $accessInsightService -notmatch 'listMembers' -or $accessInsightService -notmatch 'managedPolicyBindings\.list' -or $accessInsightService -notmatch 'managedPolicies\.listStatements') { throw "Administration access insight must remain a class-based managed-policy composition over typed administration APIs." }
if ($accessInsightService -match '\.administration\.policies') { throw "Administration access insight must not read retired legacy tenant policies." }
if ($accessInsightService -match 'authorization\.evaluate' -or $accessInsightService -match 'isAllowed\s*\(') { throw "Selected-user access insight must not evaluate the current administrator and present that result as the selected user's authorization." }
if ($securityAuditQuery -notmatch 'export\s+class\s+IdentityAccessAdminSecurityAuditQuery\b') { throw "Security-audit browser query normalization must remain a focused class." }
if ($securityAuditService -notmatch 'export\s+class\s+IdentityAccessAdminSecurityAuditService\b' -or $securityAuditService -notmatch '\.administration\.securityAudit\.list\s*\(') { throw "Security-audit data loading must remain a focused server-only class over the typed administration client." }
if ($securityAuditPresentation -notmatch 'export\s+class\s+IdentityAccessAdminSecurityAuditPresentation\b') { throw "Security-audit presentation mapping must remain isolated in its own class." }
if ($securityAuditSummary -notmatch 'export\s+class\s+IdentityAccessAdminSecurityAuditSummary\b') { throw "Security-audit summary aggregation must remain isolated in its own class." }
if ($securityAuditService -match 'authorization\.evaluate' -or $securityAuditService -match 'isAllowed\s*\(') { throw "Security-audit loading must not become a second authorization engine." }
if ($securityAuditService -match 'eventLabel\s*\(' -or $securityAuditService -match 'outcomeClassName\s*\(') { throw "Security-audit loading must not absorb presentation responsibilities." }
if ($securityAuditPresentation -match '\.client\.' -or $securityAuditSummary -match '\.client\.') { throw "Security-audit presentation and summary classes must not perform API I/O." }
if ($securityAuditPage -notmatch 'IdentityAccessAdminSecurityAuditService' -or $securityAuditPage -notmatch 'AdminSecurityAuditTimeline') { throw "Security-audit page must compose the focused loader and timeline surfaces." }
if ($securityAuditTimeline -notmatch 'IdentityAccessAdminSecurityAuditPresentation') { throw "Security-audit timeline must delegate labeling to the presentation class." }
foreach ($actionName in @(
    "updateUserAction", "updateTenantAction", "updateTenantMembershipAction", "updateGroupAction",
    "addGroupMemberAction", "removeGroupMemberAction", "createManagedPolicyAction", "updateManagedPolicyAction",
    "createManagedPolicyVersionAction", "publishManagedPolicyVersionAction", "addManagedPolicyStatementAction",
    "removeManagedPolicyStatementAction",
    "addManagedGroupPolicyBindingAction", "removeManagedGroupPolicyBindingAction",
    "updateResourceScopeAction", "updateScopeAuthorityGroupAction", "addScopeAuthorityMemberAction",
    "removeScopeAuthorityMemberAction", "updateScopeAuthorityPolicyAction", "addScopeAuthorityPolicyStatementAction",
    "removeScopeAuthorityPolicyStatementAction", "addScopeAuthorityPolicyBindingAction", "removeScopeAuthorityPolicyBindingAction",
    "createMfaPolicyAction", "updateMfaPolicyAction", "revokeMfaAuthenticatorAction", "recoveryRevokeMfaAuthenticatorAction"
)) {
    if ($actions -notmatch [regex]::Escape($actionName)) { throw "Administration Server Actions are missing $actionName." }
}
foreach ($methodName in @(
    "updateUser", "updateTenant", "updateTenantMembership", "updateGroup", "addGroupMember", "removeGroupMember",
    "addManagedGroupPolicyBinding", "removeManagedGroupPolicyBinding",
    "updateResourceScope", "updateScopeAuthorityGroup", "addScopeAuthorityMember", "removeScopeAuthorityMember",
    "updateScopeAuthorityPolicy", "addScopeAuthorityPolicyStatement", "removeScopeAuthorityPolicyStatement",
    "addScopeAuthorityPolicyBinding", "removeScopeAuthorityPolicyBinding", "createMfaPolicy", "updateMfaPolicy",
    "revokeMfaAuthenticator", "recoveryRevokeMfaAuthenticator"
)) {
    if ($mutationService -notmatch ("\b" + [regex]::Escape($methodName) + "\s*\(")) { throw "IdentityAccessAdminMutationService is missing $methodName." }
}
if ($usersPage -notmatch 'updateUserAction' -or $tenantsPage -notmatch 'updateTenantAction' -or $membershipsPage -notmatch 'updateTenantMembershipAction') { throw "Directory administration must expose server-confirmed edit operations." }
if ($usersPage -notmatch 'AdminRecordContext' -or $usersPage -notmatch '/identity/memberships\?tenantId=' -or $usersPage -notmatch '/identity/mfa\?userId=' -or $usersPage -notmatch '/identity/sessions\?userId=') { throw "User administration must retain tenant-centric membership navigation plus direct MFA and session-security detail navigation." }
if ($usersPage -notmatch 'AdminAccessInsight' -or $usersPage -notmatch 'IdentityAccessAdminAccessInsightService' -or $usersPage -notmatch 'hasTenantContext') { throw "Selected user administration must retain server-composed tenant assignment provenance without assuming a tenant context exists." }
if ($tenantsPage -notmatch 'AdminRecordContext' -or $groupsPage -notmatch 'AdminRecordContext' -or $policiesPage -notmatch 'AdminRecordContext' -or $resourceScopesPage -notmatch 'AdminRecordContext' -or $membershipsPage -notmatch 'AdminRecordContext') { throw "Administration detail and relationship pages must retain explicit selected-record context." }
if ($groupsPage -notmatch 'addGroupMemberAction' -or $groupsPage -notmatch 'removeGroupMemberAction' -or $groupsPage -notmatch 'addManagedGroupPolicyBindingAction' -or $groupsPage -notmatch 'removeManagedGroupPolicyBindingAction') { throw "Group administration must expose member operations and managed-policy binding operations." }
if ($groupsPage -match 'removeGroupPolicyBindingAction' -or $groupsPage -match '\.administration\.policies' -or $groupsPage -match 'Legacy compatibility') { throw "Group administration must not expose retired legacy tenant-policy bindings." }
if ($policiesPage -notmatch 'addManagedPolicyStatementAction' -or $policiesPage -notmatch 'removeManagedPolicyStatementAction' -or $policiesPage -notmatch 'updateManagedPolicyAction' -or $policiesPage -notmatch 'publishManagedPolicyVersionAction') { throw "Managed policy administration must expose metadata edit, draft statement add/remove, and publication operations." }
if ($actions -notmatch 'IdentityAccessAdminManagedPolicyMutationService' -or $actions -notmatch 'executeManagedPolicy') { throw "Managed policy Server Actions must delegate through the focused managed-policy mutation service." }
if ($managedPolicyReadService -notmatch 'IdentityAccessAdminPolicyBuilderService' -or $policiesPage -notmatch 'name="capability"') { throw "Managed policy administration must build exact statements from the registered capability catalog." }
foreach ($freeField in @('name="resource"', 'name="feature"', 'name="action"')) {
    if ($policiesPage -match [regex]::Escape($freeField)) { throw "Managed policy statement creation must not expose free-text capability coordinates: $freeField." }
}
if ($policiesPage -match '<AdminField[^>]*name="modelVersion"') { throw 'Managed policy version creation must not expose a free-text security-model version.' }
if ($policiesPage -notmatch '<AdminSelectField[^>]*name="modelVersion"' -or $policiesPage -notmatch 'policyBuilderModels\.map\s*\(') { throw 'Managed policy version creation must select from registered application security-model versions.' }
if ($securityModelsPage -notmatch '\.administration\.securityModels\.list\s*\(' -or $securityModelsPage -notmatch 'trn:' -or $securityModelsPage -notmatch 'not authorization decisions') { throw "Security Models administration must retain registered-catalog discovery and descriptive TRN previews." }
if ($securityModelsPage -notmatch 'registerSecurityModelManifestAction' -or $securityModelsPage -notmatch 'name="manifestFile"' -or $securityModelsPage -notmatch 'type="file"' -or $securityModelsPage -notmatch 'accept="application/json,.json"') { throw "Security Models administration must expose protected registration of a project-owned JSON manifest file." }
foreach ($authoredField in @('name="resource"', 'name="feature"', 'name="action"', 'name="modelVersion"', '<textarea')) {
    if ($securityModelsPage -match [regex]::Escape($authoredField)) { throw "Security Models administration must register complete project-owned manifests instead of authoring capability coordinates in the browser: $authoredField." }
}
if ($securityManifestParser -notmatch 'export\s+class\s+IdentityAccessAdminSecurityManifestParser\b' -or $securityManifestParser -notmatch 'JSON\.parse' -or $securityManifestParser -notmatch 'manifestFile' -or $securityManifestParser -notmatch '256 KiB') { throw "Security manifest browser input must remain a focused bounded JSON-file parser." }
if ($securityModelMutationService -notmatch 'export\s+class\s+IdentityAccessAdminSecurityModelMutationService\b' -or $securityModelMutationService -notmatch '\.administration\.securityModels\.registerManifest\s*\(' -or $securityModelMutationService -notmatch 'applicationKey') { throw "Security-model registration must remain a focused server-only mutation service over the typed registration client." }
if ($securityModelMutationService -match 'authorization\.evaluate' -or $securityModelMutationService -match 'isAllowed\s*\(' -or $securityManifestParser -match 'authorization\.evaluate' -or $securityManifestParser -match 'isAllowed\s*\(') { throw "Security-model registration and parsing must not become a second RBAC evaluator." }
if ($securityModelFailurePresentation -notmatch 'export\s+class\s+IdentityAccessAdminSecurityModelFailurePresentation\b' -or $securityModelFailurePresentation -notmatch 'httpStatus\s*===\s*409' -or $securityModelFailurePresentation -notmatch 'modelVersion') { throw "Security-model registration must retain explicit immutable-version conflict guidance." }
if ($actions -notmatch 'registerSecurityModelManifestAction' -or $actions -notmatch 'IdentityAccessAdminSecurityModelMutationService' -or $actions -notmatch 'IdentityAccessAdminSecurityModelFailurePresentation') { throw "Security-model Server Action must remain a thin adapter over focused registration and failure-presentation classes." }
if ($resourceScopesPage -notmatch 'updateResourceScopeAction') { throw "Resource-scope administration must expose optimistic-concurrency editing." }
if ($authorityPage -notmatch 'addScopeAuthorityMemberAction' -or $authorityPage -notmatch 'removeScopeAuthorityMemberAction' -or $authorityPage -notmatch 'addScopeAuthorityPolicyBindingAction' -or $authorityPage -notmatch 'removeScopeAuthorityPolicyBindingAction') { throw "Scope-authority administration must expose relationship add/remove operations." }
if ($mfaPage -notmatch 'createMfaPolicyAction' -or $mfaPage -notmatch 'updateMfaPolicyAction' -or $mfaPage -notmatch 'revokeMfaAuthenticatorAction') { throw "MFA administration must expose policy mutation and authenticator revocation controls." }
if ($mutationService -notmatch 'requireLiteralConfirmation' -or $groupsPage -notmatch 'placeholder="REMOVE"' -or $policiesPage -notmatch 'placeholder="REMOVE"') { throw "Relationship removal UI must retain explicit destructive confirmation." }
$rootLayout = Get-Content (Join-Path $nextAdmin "app/layout.tsx") -Raw
$hostSession = Get-Content (Join-Path $nextAdmin "server/IdentityAccessHostSessionService.ts") -Raw
$hostConnector = Get-Content (Join-Path $nextAdmin "server/IdentityAccessServerConnector.ts") -Raw
$loginActions = Get-Content (Join-Path $nextAdmin "app/login/actions.ts") -Raw
$loginPage = Get-Content (Join-Path $nextAdmin "app/login/page.tsx") -Raw
$loginForm = Get-Content (Join-Path $nextAdmin "app/login/LoginForm.tsx") -Raw
$recoveryActions = Get-Content (Join-Path $nextAdmin "app/recovery/actions.ts") -Raw
$recoveryPage = Get-Content (Join-Path $nextAdmin "app/recovery/page.tsx") -Raw
$recoveryForm = Get-Content (Join-Path $nextAdmin "app/recovery/RecoveryForm.tsx") -Raw
$mutationDialog = Get-Content (Join-Path $nextAdmin "components/AdminMutationDialog.tsx") -Raw
$activeNavigation = Get-Content (Join-Path $nextAdmin "components/AdminNavigationActiveLink.tsx") -Raw
$mobileNavigation = Get-Content (Join-Path $nextAdmin "components/AdminMobileNavigation.tsx") -Raw
$currentSection = Get-Content (Join-Path $nextAdmin "components/AdminCurrentSection.tsx") -Raw
$hostPackage = Get-Content (Join-Path $nextAdmin "package.json") -Raw
$hostEnvironment = Get-Content (Join-Path $nextAdmin ".env.local.example") -Raw
if ($rootLayout -notmatch 'styles/identity-access-admin\.css') { throw "The runnable host root layout must import the single shared CSS file." }
if ($layout -match 'styles/identity-access-admin\.css') { throw "Global administration CSS must be owned by the runnable root layout only." }
if ($hostSession -notmatch 'export\s+class\s+IdentityAccessHostSessionService\b') { throw "Runnable host session orchestration must remain class-based." }
if ($hostConnector -notmatch 'export\s+class\s+IdentityAccessServerConnector\b' -or $hostConnector -notmatch '@identity-access/client') { throw "Runnable host server connector must remain host-local and consume the packaged class-based client." }
if ($hostSession -match '\.\./\.\./identity-access' -or $nextTypeScriptSource -match 'from\s+["'']\.\./\.\./identity-access["'']') { throw "Runnable host must not import its server connector from outside the Next.js package root." }
if ($hostSession -notmatch '\.client\.authentication\.passwordLogin' -or $hostSession -notmatch '\.client\.oidc\.authorize' -or $hostSession -notmatch '\.client\.oidc\.exchangeAuthorizationCode') { throw "Runnable host sign-in must use the composed authentication and OIDC client classes." }
$requestCookieIndex = $hostSession.IndexOf('const cookieStore = await cookies();', [System.StringComparison]::Ordinal)
$runtimeEnvironmentIndex = $hostSession.IndexOf('requiredEnvironment("IDENTITY_ACCESS_BEARER_COOKIE_NAME")', [System.StringComparison]::Ordinal)
if ($requestCookieIndex -lt 0 -or $runtimeEnvironmentIndex -lt 0 -or $requestCookieIndex -gt $runtimeEnvironmentIndex) {
    throw "Runnable host must establish request-time context before reading Identity Access runtime environment configuration."
}
if ($hostSession -notmatch 'httpOnly\s*:\s*true' -or $hostSession -notmatch 'sameSite\s*:\s*"lax"') { throw "Runnable host authentication cookies must remain HTTP-only and SameSite=Lax." }
if ($hostSession -match 'NEXT_PUBLIC_') { throw "Runnable host security configuration must remain server-only." }
if ($loginActions -notmatch '^"use server";' -or $loginActions -match '\.client\.') { throw "Login/logout Server Actions must remain thin adapters over IdentityAccessHostSessionService." }
if ($loginPage -notmatch 'LoginForm') { throw "Runnable host login page must render the dedicated login form." }
if ($loginForm -notmatch 'href="/recovery"' -or $loginForm -notmatch 'aria-pressed' -or $loginForm -notmatch 'aria-busy=\{pending\}') { throw "Runnable host sign-in must expose account recovery, accessible password visibility, and pending-state semantics." }
if ($recoveryActions -notmatch '^"use server";' -or $recoveryActions -match '\.client\.' -or $recoveryActions -notmatch 'IdentityAccessHostSessionService') { throw "Recovery Server Action must remain a thin adapter over IdentityAccessHostSessionService." }
if ($recoveryPage -notmatch 'RecoveryForm') { throw "Runnable host recovery page must render the dedicated recovery form." }
foreach ($field in @("loginIdentifier", "recoveryCode", "newPassword", "confirmPassword")) {
    if ($recoveryForm -notmatch [regex]::Escape($field)) { throw "Recovery form is missing $field." }
}
if ($recoveryForm -match 'authenticatorId') { throw "Public account recovery must not ask the browser for a recovery authenticator identifier." }
if ($recoveryForm -notmatch 'aria-busy=\{pending\}' -or $recoveryForm -notmatch 'aria-describedby="recovery-code-hint"' -or $recoveryForm -notmatch 'spellCheck=\{false\}') { throw "Public account recovery must retain pending-state semantics and an explicitly described recovery-code field." }
if ($hostSession -notmatch '\.client\.authentication\.recoverPasswordWithCode') { throw "Runnable host account recovery must use the composed authentication client recovery contract." }
if ($activeNavigation -notmatch 'usePathname' -or $activeNavigation -notmatch 'aria-current') { throw "Administration navigation must expose client-only active-route state with aria-current." }
if ($mobileNavigation -notmatch 'export\s+function\s+AdminMobileNavigation\b' -or $mobileNavigation -notmatch 'usePathname' -or $mobileNavigation -notmatch 'onToggle' -or $mobileNavigation -notmatch 'aria-expanded' -or $mobileNavigation -notmatch 'aria-controls') { throw "Compact administration navigation must retain route-aware disclosure state with explicit accessibility relationships." }
if ($mobileNavigation -notmatch 'details\.current\.open\s*=\s*false') { throw "Compact administration navigation must close after a route transition." }
if ($currentSection -notmatch 'usePathname') { throw "Administration top-bar context must resolve the current visible workspace from the current route." }
if ($mutationDialog -notmatch 'aria-labelledby' -or $mutationDialog -notmatch 'aria-describedby' -or $mutationDialog -notmatch 'aria-busy' -or $mutationDialog -notmatch 'ia-destructive-notice') { throw "Administration mutation dialogs must retain accessible dialog relationships, pending semantics, and explicit security-sensitive guidance." }
if ($mutationDialog -notmatch 'AdminFailureFeedback' -or $mutationDialog -notmatch 'state\.failure') { throw "Administration mutation dialogs must render structured server-classified failure feedback." }
if ($mutationDialog -notmatch 'trigger\.current\?\.focus\(\)' -or $mutationDialog -notmatch 'event\.preventDefault\(\)') { throw "Administration mutation dialogs must restore trigger focus on controlled close and retain explicit cancel handling." }
if ($layout -notmatch 'IdentityAccessClientError' -or $layout -notmatch 'error\.code\s*===\s*"unauthenticated"' -or $layout -notmatch '/login\?session=expired') { throw "Protected administration layout must route invalid authenticated sessions back to sign-in without converting them into a generic authorization result." }
if ($loginPage -notmatch 'session\?:\s*string' -or $loginPage -notmatch 'session\s*===\s*"expired"' -or $loginPage -notmatch 'ia-auth-notice') { throw "Sign-in UX must explain an expired/revoked administration context without exposing token details." }
if ($layout -notmatch 'AdminCurrentSection' -or $layout -notmatch 'AdminMobileNavigation' -or $layout -notmatch 'id="identity-main"' -or $layout -notmatch 'tabIndex=\{-1\}') { throw "Protected administration layout must retain current-workspace context, route-aware compact navigation, and a focusable skip target." }
if ($hostPackage -notmatch '"@identity-access/client"\s*:\s*"file:\.\./\.\./\.\./clients/typescript"') { throw "Runnable host must consume the local class-based TypeScript client package." }
if ($hostPackage -notmatch '"next"\s*:\s*"16\.3\.6"') { throw "Runnable host must pin the qualified Next.js Active LTS security release." }
if ($hostPackage -notmatch '"react"\s*:\s*"19\.3\.0"' -or $hostPackage -notmatch '"react-dom"\s*:\s*"19\.3\.0"') { throw "Runnable host React runtime versions are not pinned." }
foreach ($name in @("IDENTITY_ACCESS_API_BASE_URL", "IDENTITY_ACCESS_OIDC_CLIENT_ID", "IDENTITY_ACCESS_OIDC_REDIRECT_URI", "IDENTITY_ACCESS_BEARER_COOKIE_NAME")) {
    if ($hostEnvironment -notmatch [regex]::Escape($name)) { throw "Runnable host environment example is missing $name." }
}
if ($hostEnvironment -match 'NEXT_PUBLIC_') { throw "Runnable host environment must not expose Identity Access security configuration through NEXT_PUBLIC_." }
if ($overviewPage -match 'redirect\s*\(') { throw "The premium administration overview must be a real dashboard page instead of redirecting immediately." }
if ($overviewPage -notmatch 'AdminMetricCard' -or $overviewPage -notmatch 'AdminFeatureCard') { throw "The premium overview must retain metric and workspace cards." }
if ($detailCard -notmatch 'export\s+function\s+AdminDetailCard\b') { throw "Structured administration details must use the shared AdminDetailCard component." }
if ($adminField -notmatch 'export\s+function\s+AdminField\b') { throw "Shared administration text input must export AdminField." }
if ($adminField -notmatch 'useId' -or $adminField -notmatch 'mergeDescribedBy' -or $adminField -notmatch 'aria-describedby=\{describedBy\}') { throw "Shared administration fields must explicitly associate hint text without replacing caller-provided accessible descriptions." }
if ($entityAutocomplete -notmatch 'export\s+function\s+AdminEntityAutocomplete\b' -or $entityAutocomplete -notmatch 'role="combobox"' -or $entityAutocomplete -notmatch 'aria-autocomplete="list"' -or $entityAutocomplete -notmatch '<input type="hidden" name=\{name\} value=\{selectedId\}') { throw "Administration relationship references must use the shared autocomplete and submit only the selected stable identifier." }
if ($entityAutocomplete -notmatch 'option\.displayName' -or $entityAutocomplete -notmatch 'option\.id' -or $entityAutocomplete -notmatch 'aria-activedescendant') { throw "Administration entity autocomplete must expose display name, stable ID, and keyboard listbox relationships." }
if ($entityAutocomplete -match '\.client\.' -or $entityAutocomplete -match 'authorization\.evaluate' -or $entityAutocomplete -match 'isAllowed\s*\(') { throw "The shared autocomplete must remain a presentation/interaction component and must not perform API or authorization work." }
if ($entityAutocomplete -notmatch 'const\s+minimumSearchLength\s*=\s*3' -or $entityAutocomplete -notmatch 'const\s+debounceMilliseconds\s*=\s*250' -or $entityAutocomplete -notmatch 'new\s+AbortController\s*\(' -or $entityAutocomplete -notmatch 'slice\(0,\s*20\)' -or $entityAutocomplete -notmatch '/api/identity/entity-references') { throw "Administration entity autocomplete must use bounded debounced server-backed search after three characters." }
if ($entityAutocomplete -notmatch 'const\s+hasResolvedSelection\s*=' -or $entityAutocomplete -notmatch 'if\s*\(!hasResolvedSelection\)\s*setOpen\(true\)' -or $entityAutocomplete -notmatch 'selected\.`') { throw "Administration entity autocomplete must keep a resolved selection closed and must not report an empty-search state for the selected record." }
if ($nextTypeScriptSource -match '<AdminEntityAutocomplete[^>]+\boptions=') { throw "Administration entity autocomplete must not preload relationship option collections into the browser." }
if ($groupsPage -match 'resourceScopeId\s*!==\s*null') { throw "Optional group policy-binding resource scopes must not use a null-only presence check; the public client omits absent scope identifiers." }
if ($groupsPage -notmatch 'resourceScopeId\s*!==\s*undefined') { throw "Group policy-binding scope resolution must guard the optional resourceScopeId before typed client lookup." }
if ($entityReferencePresentation -notmatch 'export\s+class\s+IdentityAccessAdminEntityReferencePresentation\b' -or $entityReferencePresentation -notmatch 'static\s+users\s*\(' -or $entityReferencePresentation -notmatch 'static\s+tenantMemberships\s*\(' -or $entityReferencePresentation -notmatch 'static\s+resourceScopes\s*\(') { throw "Entity reference presentation must remain a focused class over typed administration records." }
if ($entityReferenceSearchService -notmatch 'export\s+class\s+IdentityAccessAdminEntityReferenceSearchService\b' -or $entityReferenceSearchService -notmatch 'minimumSearchLength\s*=\s*3' -or $entityReferenceSearchService -notmatch 'maximumResults\s*=\s*20' -or $entityReferenceSearchService -notmatch '\.list\([^\)]*options' -or $entityReferenceSearchService -match 'authorization\.evaluate|isAllowed\s*\(') { throw "Entity-reference lookup must remain a focused bounded server-side search class without RBAC reimplementation." }
if ($entityReferenceRoute -notmatch 'IdentityAccessAdminEntityReferenceSearchService' -or $entityReferenceRoute -notmatch 'export\s+async\s+function\s+GET\b' -or $entityReferenceRoute -match '\.list\(') { throw "Entity-reference API route must remain a thin adapter over the server-side search class." }
if ($membershipsPage -notmatch 'IdentityAccessAdminMembershipOverviewService' -or $membershipsPage -notmatch 'AdminMembershipTenantTable' -or $membershipsPage -notmatch 'AdminMembershipMemberTable' -or $membershipsPage -notmatch 'overview\.scopeWide') { throw "Tenant membership administration must retain the tenant-centric scoped overview instead of the legacy tenant/user lookup workflow." }
if ($mfaPage -match 'users\.list\([^\)]*limit:\s*200' -or $authorityPage -match '(?:users\.list|scopeAuthority\.listGroups|scopeAuthority\.listPolicies)\([^\)]*limit:\s*200') { throw "MFA and scope-authority autocompletes must not preload directory catalogs." }
foreach ($requiredUsage in @(
    @{ Source = $groupsPage; Pattern = 'AdminEntityAutocomplete[^>]+name="tenantMembershipId"'; Label = 'group tenant member' },
    @{ Source = $groupsPage; Pattern = 'AdminEntityAutocomplete[^>]+name="managedPolicyId"[^>]+kind="managed-policy"'; Label = 'managed group policy binding' },
    @{ Source = $groupsPage; Pattern = 'AdminEntityAutocomplete[^>]+name="resourceScopeId"'; Label = 'group resource scope' },
    @{ Source = $resourceScopesPage; Pattern = 'AdminEntityAutocomplete[^>]+name="parentResourceScopeId"'; Label = 'parent resource scope' },
    @{ Source = $mfaPage; Pattern = 'AdminEntityAutocomplete[^>]+name="userId"'; Label = 'MFA user' },
    @{ Source = $sessionsPage; Pattern = 'AdminEntityAutocomplete[^>]+name="userId"'; Label = 'session user' },
    @{ Source = $securityAuditPage; Pattern = 'AdminEntityAutocomplete[^>]+name="userId"'; Label = 'security-audit user' },
    @{ Source = $securityAuditPage; Pattern = 'AdminEntityAutocomplete[^>]+name="tenantId"'; Label = 'security-audit tenant' },
    @{ Source = $authorityPage; Pattern = 'AdminEntityAutocomplete[^>]+name="groupId"'; Label = 'authority group' },
    @{ Source = $authorityPage; Pattern = 'AdminEntityAutocomplete[^>]+name="policyId"'; Label = 'authority policy' },
    @{ Source = $authorityPage; Pattern = 'AdminEntityAutocomplete[^>]+name="userId"'; Label = 'authority user' }
)) {
    if ($requiredUsage.Source -notmatch $requiredUsage.Pattern) { throw "Editable $($requiredUsage.Label) references must use AdminEntityAutocomplete." }
}
$editableRawReferencePattern = '<Admin(?:Field|SelectField)[^>]+name="(?:userId|tenantId|membershipId|tenantMembershipId|groupId|policyId|managedPolicyId|resourceScopeId|parentResourceScopeId)"'
if ($nextTypeScriptSource -match $editableRawReferencePattern) { throw "Editable administrable foreign identifiers must not regress to raw text/select fields." }
if ($mutationService -match 'error\.kind\b') { throw "IdentityAccessClientError must be inspected through its public code property." }
if ($nextTypeScriptSource -match '\bprivate\s+(?:async\s+)?#') { throw "TypeScript private identifiers must not be combined with a private accessibility modifier." }
if ($localClientBuild -notmatch 'node_modules/@identity-access/client' -or $localClientBuild -notmatch 'cpSync\(clientDist') { throw "Runnable host local client bootstrap must synchronize the freshly built package into node_modules." }
if ($localClientBuild -match 'realpathSync' -or $localClientBuild -match 'linkedToSource') { throw "Runnable host must materialize the local client package instead of preserving an external symlink/junction." }
if ($localClientBuild -notmatch 'lstatSync' -or $localClientBuild -notmatch 'isSymbolicLink\(\)' -or $localClientBuild -notmatch 'unlinkSync') { throw "Runnable host local client bootstrap must remove npm symlink/junction layouts before materialization." }
if ($localClientBuild -notmatch 'dist/index\.js' -or $localClientBuild -notmatch 'dist/index\.d\.ts') { throw "Runnable host local client bootstrap must verify runtime and declaration entry points after materialization." }
if ($adminCss -notmatch '--ia-accent' -or $adminCss -notmatch '\.ia-overview-hero') { throw "The premium administration design tokens and overview surface are missing." }
if ($adminCss -notmatch '@media\s*\(prefers-color-scheme:\s*dark\)') { throw "The premium administration stylesheet must retain automatic dark-mode support." }
if ($adminCss -notmatch '@media\s*\(prefers-reduced-motion:\s*reduce\)') { throw "The premium administration stylesheet must respect reduced-motion preferences." }
if ($adminCss -notmatch '\.ia-login-shell' -or $adminCss -notmatch '\.ia-login-card') { throw "The runnable administration host login surface is missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-navigation-link-active' -or $adminCss -notmatch '\.ia-mobile-navigation-popover') { throw "Administration active-route and compact mobile navigation styles are missing." }
if ($adminCss -notmatch '\.ia-recovery-shell' -or $adminCss -notmatch '\.ia-password-toggle') { throw "Authentication recovery and password-control styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-management-workspace' -or $adminCss -notmatch '\.ia-row-actions' -or $adminCss -notmatch '\.ia-checkbox-field') { throw "Administration CRUD and relationship-management styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-table-toolbar-controls' -or $adminCss -notmatch '\.ia-destructive-notice' -or $adminCss -notmatch '@keyframes\s+ia-dialog-enter') { throw "Administration visual and interaction polish styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-record-context' -or $adminCss -notmatch '\.ia-table-row-selected' -or $adminCss -notmatch '\.ia-selected-badge') { throw "Administration detail and selected-record context styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-access-insight' -or $adminCss -notmatch '\.ia-access-path-group' -or $adminCss -notmatch '\.ia-capability-pattern') { throw "Administration access-provenance insight styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-audit-timeline' -or $adminCss -notmatch '\.ia-audit-filter-grid' -or $adminCss -notmatch '\.ia-audit-event') { throw "Security-audit administration styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-session-filter-grid' -or $adminCss -notmatch '\.ia-session-operations' -or $adminCss -notmatch '\.ia-session-event-actions') { throw "Session security operations styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-failure-feedback' -or $adminCss -notmatch '\.ia-failure-conflict' -or $adminCss -notmatch '\.ia-auth-notice') { throw "Production failure and expired-session UX styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '\.ia-entity-autocomplete' -or $adminCss -notmatch '\.ia-entity-autocomplete-list' -or $adminCss -notmatch '\.ia-entity-autocomplete-option-active') { throw "Administration entity autocomplete styles are missing from the single shared stylesheet." }
if ($adminCss -notmatch '@media\s*\(forced-colors:\s*active\)' -or $adminCss -notmatch '@media\s*\(prefers-contrast:\s*more\)' -or $adminCss -notmatch '@supports\s*\(min-height:\s*100dvh\)' -or $adminCss -notmatch '\.ia-main:focus-visible') { throw "Final administration accessibility styles must retain forced-colors, increased-contrast, dynamic-viewport, and skip-target focus behavior." }
$cssFiles = @(Get-NextAdminOwnedFiles -Extensions @(".css"))
if ($cssFiles.Count -ne 1) { throw "Next.js administration must own exactly one CSS file; found $($cssFiles.Count)." }
if ($cssFiles[0].Name -ne 'identity-access-admin.css') { throw "The single administration CSS file must be identity-access-admin.css." }
$tsxSource = (Get-NextAdminOwnedFiles -Extensions @(".tsx") | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
if ($tsxSource -match 'JSON\.stringify\s*\(') { throw "Premium administration pages must render structured record details instead of raw JSON dumps." }
if ($tsxSource -match 'style\s*=\s*\{') { throw "Inline React style objects are not allowed in the administration module; use the single shared CSS file." }
if ($tsxSource -match '<style[ >]') { throw "Component-local style blocks are not allowed in the administration module." }
$moduleCss = @(Get-NextAdminOwnedFiles -Extensions @(".css") | Where-Object { $_.Name -like "*.module.css" })
if ($moduleCss.Count -ne 0) { throw "CSS Modules are not allowed in the administration module." }
Write-Host "TypeScript source consistency validation passed."
