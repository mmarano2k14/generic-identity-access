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
    "client/administration/IdentityAccessAdministrationTransport.ts",
    "client/administration/IdentityAccessUsersClient.ts",
    "client/administration/IdentityAccessTenantsClient.ts",
    "client/administration/IdentityAccessMembershipsClient.ts",
    "client/administration/IdentityAccessMfaClient.ts",
    "client/administration/IdentityAccessGroupsClient.ts",
    "client/administration/IdentityAccessPoliciesClient.ts",
    "client/administration/IdentityAccessResourceScopesClient.ts",
    "client/administration/IdentityAccessSecurityModelsClient.ts",
    "client/administration/IdentityAccessSessionsClient.ts",
    "client/administration/IdentityAccessScopeAuthorityClient.ts"
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
$usersClient = Get-Content (Join-Path $src "client/administration/IdentityAccessUsersClient.ts") -Raw
$tenantsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessTenantsClient.ts") -Raw
$membershipsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessMembershipsClient.ts") -Raw
$mfaClient = Get-Content (Join-Path $src "client/administration/IdentityAccessMfaClient.ts") -Raw
$groupsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessGroupsClient.ts") -Raw
$policiesClient = Get-Content (Join-Path $src "client/administration/IdentityAccessPoliciesClient.ts") -Raw
$resourceScopesClient = Get-Content (Join-Path $src "client/administration/IdentityAccessResourceScopesClient.ts") -Raw
$securityModelsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessSecurityModelsClient.ts") -Raw
$sessionsClient = Get-Content (Join-Path $src "client/administration/IdentityAccessSessionsClient.ts") -Raw
$scopeAuthorityClient = Get-Content (Join-Path $src "client/administration/IdentityAccessScopeAuthorityClient.ts") -Raw
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
    @{ Source = $systemClient; ClassName = "IdentityAccessSystemClient"; Methods = @("liveness", "readiness", "info") },
    @{ Source = $authenticationClient; ClassName = "IdentityAccessAuthenticationClient"; Methods = @("passwordLogin", "validateSession", "logout") },
    @{ Source = $oidcClient; ClassName = "IdentityAccessOidcClient"; Methods = @("authorize", "exchangeAuthorizationCode", "refreshTokens") },
    @{ Source = $authorizationClient; ClassName = "IdentityAccessAuthorizationClient"; Methods = @("validateContext", "evaluate") },
    @{ Source = $usersClient; ClassName = "IdentityAccessUsersClient"; Methods = @("list", "get", "create", "update") },
    @{ Source = $tenantsClient; ClassName = "IdentityAccessTenantsClient"; Methods = @("list", "get", "create", "update") },
    @{ Source = $membershipsClient; ClassName = "IdentityAccessMembershipsClient"; Methods = @("get", "findByUser", "create", "update") },
    @{ Source = $mfaClient; ClassName = "IdentityAccessMfaClient"; Methods = @("listProviders", "getPolicy", "createPolicy", "updatePolicy", "listAuthenticators", "getUserSecurityState", "revokeAuthenticator", "revokeAuthenticatorForRecovery") },
    @{ Source = $groupsClient; ClassName = "IdentityAccessGroupsClient"; Methods = @("list", "get", "create", "update", "listMembers", "addMember", "removeMember") },
    @{ Source = $policiesClient; ClassName = "IdentityAccessPoliciesClient"; Methods = @("list", "get", "create", "update", "listStatements", "addStatement", "removeStatement", "listBindings", "addBinding", "removeBinding") },
    @{ Source = $resourceScopesClient; ClassName = "IdentityAccessResourceScopesClient"; Methods = @("list", "get", "create", "update") },
    @{ Source = $securityModelsClient; ClassName = "IdentityAccessSecurityModelsClient"; Methods = @("listScopeTypes", "addScopeType") },
    @{ Source = $sessionsClient; ClassName = "IdentityAccessSessionsClient"; Methods = @("revokeUser", "revokeClient") },
    @{ Source = $scopeAuthorityClient; ClassName = "IdentityAccessScopeAuthorityClient"; Methods = @("getGroup", "createGroup", "updateGroup", "listMembers", "addMember", "removeMember", "getPolicy", "createPolicy", "updatePolicy", "listPolicyStatements", "addPolicyStatement", "removePolicyStatement", "listPolicyBindings", "addPolicyBinding", "removePolicyBinding") }
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

if ($administrationClient -notmatch 'public\s+readonly\s+users\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+mfa\s*:' -or $administrationClient -notmatch 'public\s+readonly\s+scopeAuthority\s*:') {
    throw "IdentityAccessAdministrationClient must compose the focused administration clients."
}
if ($allSource -match 'client_secret') { throw "The TypeScript public-client implementation must not introduce client_secret." }
if ($valueCodec -notmatch 'capabilityPatternSegment') { throw "Typed policy administration must preserve supported wildcard capability patterns." }
if ($context -notmatch '\.authorization\.evaluate\s*\(') { throw "IdentityAuthorizationContext must delegate through IdentityAccessAuthorizationClient." }
if ($builder -notmatch '\bwithAll\s*\(') { throw "IdentityAccessAdminUiBuilder.withAll is required." }
if ($builder -notmatch '\bwithTenantAuthorization\s*\(') { throw "Tenant-scoped UI visibility must use an explicit tenant authorization context." }
if ($builder -notmatch '\bhref\s*:') { throw "Administration UI entries must carry stable route metadata." }
$nextAdmin = Join-Path $root "examples/nextjs/admin"
$requiredNextFiles = @(
    "server/IdentityAccessAdminRequest.ts",
    "server/IdentityAccessAdminMutationService.ts",
    "server/IdentityAccessServerConnector.ts",
    "contracts/AdminActionState.ts",
    "components/AdminNavigation.tsx",
    "components/AdminNavigationActiveLink.tsx",
    "components/AdminCurrentSection.tsx",
    "components/AdminIcon.tsx",
    "components/AdminMetricCard.tsx",
    "components/AdminFeatureCard.tsx",
    "components/AdminDetailCard.tsx",
    "components/AdminSecurityBanner.tsx",
    "components/AdminEntityTable.tsx",
    "components/AdminMutationDialog.tsx",
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
    "app/identity/resource-scopes/page.tsx",
    "app/identity/mfa/page.tsx",
    "app/identity/sessions/page.tsx",
    "app/identity/authority/page.tsx",
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
$actions = Get-Content (Join-Path $nextAdmin "app/identity/actions.ts") -Raw
$layout = Get-Content (Join-Path $nextAdmin "app/identity/layout.tsx") -Raw
$sessionsPage = Get-Content (Join-Path $nextAdmin "app/identity/sessions/page.tsx") -Raw
$overviewPage = Get-Content (Join-Path $nextAdmin "app/identity/page.tsx") -Raw
$adminCss = Get-Content (Join-Path $nextAdmin "styles/identity-access-admin.css") -Raw
$detailCard = Get-Content (Join-Path $nextAdmin "components/AdminDetailCard.tsx") -Raw
$adminField = Get-Content (Join-Path $nextAdmin "components/AdminField.tsx") -Raw
$entityTable = Get-Content (Join-Path $nextAdmin "components/AdminEntityTable.tsx") -Raw
$usersPage = Get-Content (Join-Path $nextAdmin "app/identity/users/page.tsx") -Raw
$tenantsPage = Get-Content (Join-Path $nextAdmin "app/identity/tenants/page.tsx") -Raw
$membershipsPage = Get-Content (Join-Path $nextAdmin "app/identity/memberships/page.tsx") -Raw
$groupsPage = Get-Content (Join-Path $nextAdmin "app/identity/groups/page.tsx") -Raw
$policiesPage = Get-Content (Join-Path $nextAdmin "app/identity/policies/page.tsx") -Raw
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
if ($actions -notmatch '^"use server";') { throw "Next.js administration actions must be explicit server actions." }
if ($actions -match '\.client\.') { throw "Next.js Server Action adapters must delegate through IdentityAccessAdminMutationService instead of calling IdentityAccessClient directly." }
if ($mutationService -notmatch 'requireConfirmation') { throw "Security-sensitive administration mutations must retain explicit confirmation validation." }
if ($sessionsPage -notmatch 'confirmation' -or $sessionsPage -notmatch 'dangerous') { throw "Session revocation UI must retain explicit destructive confirmation." }
if ($entityTable -notmatch 'actions\?:\s*ReactNode' -or $entityTable -notmatch 'ia-row-actions') { throw "Administration entity tables must expose reusable per-record action surfaces." }
if ($entityTable -notmatch 'aria-live' -or $entityTable -notmatch 'All statuses' -or $entityTable -notmatch 'Newest version' -or $entityTable -notmatch 'data-label="Identifier"') { throw "Administration entity tables must retain accessible filtering, presentation sorting, live result counts, and responsive data labels." }
foreach ($actionName in @(
    "updateUserAction", "updateTenantAction", "updateTenantMembershipAction", "updateGroupAction",
    "addGroupMemberAction", "removeGroupMemberAction", "updatePolicyAction", "addPolicyStatementAction",
    "removePolicyStatementAction", "addGroupPolicyBindingAction", "removeGroupPolicyBindingAction",
    "updateResourceScopeAction", "updateScopeAuthorityGroupAction", "addScopeAuthorityMemberAction",
    "removeScopeAuthorityMemberAction", "updateScopeAuthorityPolicyAction", "addScopeAuthorityPolicyStatementAction",
    "removeScopeAuthorityPolicyStatementAction", "addScopeAuthorityPolicyBindingAction", "removeScopeAuthorityPolicyBindingAction",
    "createMfaPolicyAction", "updateMfaPolicyAction", "revokeMfaAuthenticatorAction", "recoveryRevokeMfaAuthenticatorAction"
)) {
    if ($actions -notmatch [regex]::Escape($actionName)) { throw "Administration Server Actions are missing $actionName." }
}
foreach ($methodName in @(
    "updateUser", "updateTenant", "updateTenantMembership", "updateGroup", "addGroupMember", "removeGroupMember",
    "updatePolicy", "addPolicyStatement", "removePolicyStatement", "addGroupPolicyBinding", "removeGroupPolicyBinding",
    "updateResourceScope", "updateScopeAuthorityGroup", "addScopeAuthorityMember", "removeScopeAuthorityMember",
    "updateScopeAuthorityPolicy", "addScopeAuthorityPolicyStatement", "removeScopeAuthorityPolicyStatement",
    "addScopeAuthorityPolicyBinding", "removeScopeAuthorityPolicyBinding", "createMfaPolicy", "updateMfaPolicy",
    "revokeMfaAuthenticator", "recoveryRevokeMfaAuthenticator"
)) {
    if ($mutationService -notmatch ("\b" + [regex]::Escape($methodName) + "\s*\(")) { throw "IdentityAccessAdminMutationService is missing $methodName." }
}
if ($usersPage -notmatch 'updateUserAction' -or $tenantsPage -notmatch 'updateTenantAction' -or $membershipsPage -notmatch 'updateTenantMembershipAction') { throw "Directory administration must expose server-confirmed edit operations." }
if ($groupsPage -notmatch 'addGroupMemberAction' -or $groupsPage -notmatch 'removeGroupMemberAction' -or $groupsPage -notmatch 'addGroupPolicyBindingAction' -or $groupsPage -notmatch 'removeGroupPolicyBindingAction') { throw "Group administration must expose add/remove member and policy-binding operations." }
if ($policiesPage -notmatch 'addPolicyStatementAction' -or $policiesPage -notmatch 'removePolicyStatementAction' -or $policiesPage -notmatch 'updatePolicyAction') { throw "Policy administration must expose edit and statement add/remove operations." }
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
if ($loginForm -notmatch 'href="/recovery"' -or $loginForm -notmatch 'aria-pressed') { throw "Runnable host sign-in must expose account recovery and an accessible password visibility control." }
if ($recoveryActions -notmatch '^"use server";' -or $recoveryActions -match '\.client\.' -or $recoveryActions -notmatch 'IdentityAccessHostSessionService') { throw "Recovery Server Action must remain a thin adapter over IdentityAccessHostSessionService." }
if ($recoveryPage -notmatch 'RecoveryForm') { throw "Runnable host recovery page must render the dedicated recovery form." }
foreach ($field in @("loginIdentifier", "recoveryCode", "newPassword", "confirmPassword")) {
    if ($recoveryForm -notmatch [regex]::Escape($field)) { throw "Recovery form is missing $field." }
}
if ($recoveryForm -match 'authenticatorId') { throw "Public account recovery must not ask the browser for a recovery authenticator identifier." }
if ($hostSession -notmatch '\.client\.authentication\.recoverPasswordWithCode') { throw "Runnable host account recovery must use the composed authentication client recovery contract." }
if ($activeNavigation -notmatch 'usePathname' -or $activeNavigation -notmatch 'aria-current') { throw "Administration navigation must expose client-only active-route state with aria-current." }
if ($currentSection -notmatch 'usePathname') { throw "Administration top-bar context must resolve the current visible workspace from the current route." }
if ($mutationDialog -notmatch 'aria-labelledby' -or $mutationDialog -notmatch 'aria-describedby' -or $mutationDialog -notmatch 'aria-busy' -or $mutationDialog -notmatch 'ia-destructive-notice') { throw "Administration mutation dialogs must retain accessible dialog relationships, pending semantics, and explicit security-sensitive guidance." }
if ($layout -notmatch 'AdminCurrentSection' -or $layout -notmatch 'ia-mobile-navigation' -or $layout -notmatch 'id="identity-main"') { throw "Protected administration layout must retain current-workspace context, compact mobile navigation, and a skip target." }
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
