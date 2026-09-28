[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-RequiredFile([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "R4-C source is incomplete: missing $relativePath."
    }
    return Get-Content -LiteralPath $path -Raw
}

$candidateService = Read-RequiredFile "src/IdentityAccess.Application/Administration/TenantMembershipCandidateService.cs"
$candidateController = Read-RequiredFile "src/IdentityAccess.Api/Controllers/TenantMembershipCandidatesController.cs"
$assignmentController = Read-RequiredFile "src/IdentityAccess.Api/Controllers/TenantGroupAssignmentsController.cs"
$membershipStore = Read-RequiredFile "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlGroupMembershipMutationStore.cs"
$adminClient = Read-RequiredFile "clients/typescript/src/client/administration/IdentityAccessAdministrationClient.ts"
$candidateClient = Read-RequiredFile "clients/typescript/src/client/administration/IdentityAccessMembershipCandidatesClient.ts"
$assignmentClient = Read-RequiredFile "clients/typescript/src/client/administration/IdentityAccessTenantGroupAssignmentsClient.ts"
$exactLookup = Read-RequiredFile "examples/nextjs/admin/components/AdminExactMemberLookup.tsx"
$addMember = Read-RequiredFile "examples/nextjs/admin/components/AdminAddTenantMemberDialog.tsx"
$manageGroups = Read-RequiredFile "examples/nextjs/admin/components/AdminManageMemberGroupsDialog.tsx"
$membershipPage = Read-RequiredFile "examples/nextjs/admin/app/identity/memberships/page.tsx"
$mutationService = Read-RequiredFile "examples/nextjs/admin/server/IdentityAccessAdminMutationService.ts"
$overviewService = Read-RequiredFile "examples/nextjs/admin/server/IdentityAccessAdminMembershipOverviewService.ts"
$candidateRoute = Read-RequiredFile "examples/nextjs/admin/app/api/identity/membership-candidates/route.ts"
$delegationGuard = Read-RequiredFile "src/IdentityAccess.Api/Security/TenantGroupAssignmentDelegationGuard.cs"
$groupGrantReader = Read-RequiredFile "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlGroupCapabilityGrantReader.cs"
$groupMembersController = Read-RequiredFile "src/IdentityAccess.Api/Controllers/GroupMembersController.cs"
$membershipsController = Read-RequiredFile "src/IdentityAccess.Api/Controllers/TenantMembershipsController.cs"
$membershipCreateGuard = Read-RequiredFile "src/IdentityAccess.Api/Security/TenantMembershipCreationAuthorizationGuard.cs"

# Exact-login resolution is intentionally a bounded point lookup, never a tenant-side global directory search.
if ($candidateService -notmatch 'FindByLoginAsync' -or $candidateService -notmatch 'credentials\.FindByLoginAsync') {
    throw "Tenant membership candidate resolution must use the exact credential-login lookup."
}
if ($candidateService -match 'ListUsersAsync|ListTenantUsersAsync|users\.ListAsync') {
    throw "Tenant membership candidate resolution must not become a browsable global-user search."
}
if ($candidateController -notmatch 'TenantMemberships.*Write' -or $candidateController -notmatch 'HttpGet\("by-login"\)' -or $candidateController -notmatch 'HttpPost\("by-login/membership"\)') {
    throw "Exact-login membership lookup and tenant-scoped creation must remain capability-protected and explicit."
}
if ($candidateClient -notmatch 'findByLogin\s*\(' -or $candidateClient -notmatch 'createMembershipByLogin\s*\(' -or $candidateClient -notmatch 'membership-candidates/by-login/membership') {
    throw "The TypeScript connector must expose exact-login membership resolution and tenant-scoped creation."
}
if ($candidateRoute -notmatch 'membershipCandidates\.findByLogin' -or $candidateRoute -match 'kind="user"|entity-references') {
    throw "The tenant-scoped candidate route must use exact-login resolution rather than global autocomplete."
}
if ($exactLookup -notmatch 'Find account' -or $exactLookup -notmatch 'loginIdentifier' -or $exactLookup -match 'AdminEntityAutocomplete') {
    throw "Tenant-scoped Add member must use an explicit exact-login lookup with no global autocomplete."
}
if ($addMember -notmatch 'scopeWide\s*\?' -or $addMember -notmatch 'kind="user"' -or $addMember -notmatch 'AdminExactMemberLookup') {
    throw "Add member must distinguish scope-wide user search from tenant-scoped exact-login resolution."
}
if ($mutationService -notmatch 'tenantVisibility\s*!==\s*"scope-wide"' -or $mutationService -notmatch 'membershipCandidates\.findByLogin' -or $mutationService -notmatch 'candidate\.userId\s*!==\s*userId' -or $mutationService -notmatch 'membershipCandidates\.createMembershipByLogin') {
    throw "Tenant-scoped membership creation must revalidate and create through the exact-login server path."
}
if ($membershipsController -notmatch 'AuthorizeDirectCreateAsync' -or $membershipsController -notmatch 'Tenant-scoped administrators must add members through exact-login resolution') {
    throw "Direct user-id membership creation must be restricted to identity-scope administration."
}
if ($membershipCreateGuard -notmatch 'scopeAuthorization\.AuthorizeAsync' -or $membershipCreateGuard -notmatch 'TenantMemberships' -or $membershipCreateGuard -notmatch 'Write') {
    throw "Direct membership creation guard must require identity-scope tenant-membership/write authority."
}

# Aggregate group assignment read remains tenant/application scoped and capability protected.
if ($assignmentController -notmatch 'GroupMemberships.*Read' -or $assignmentController -notmatch 'ListTenantGroupAssignmentsAsync') {
    throw "Tenant group assignment aggregation must remain read-capability protected."
}
if ($assignmentClient -notmatch 'tenantApplicationPath' -or $assignmentClient -notmatch '/group-memberships') {
    throw "The TypeScript group-assignment aggregate must remain inside the tenant/application path."
}
if ($adminClient -notmatch 'membershipCandidates' -or $adminClient -notmatch 'tenantGroupAssignments') {
    throw "IdentityAccessAdministrationClient must compose the R4-C focused clients."
}

# PostgreSQL mutation uses group tenant as authority and joins the target membership to the same tenant.
if ($membershipStore -notmatch 'tm\.tenant_id\s*=\s*g\.tenant_id' -or $membershipStore -notmatch 'tm\.membership_id\s*=\s*@membership_id') {
    throw "Group assignment must prove the target membership belongs to the same tenant as the group."
}
if ($groupMembersController -notmatch 'AuthorizeAssignmentAsync' -or $groupMembersController -notmatch 'Group assignment delegation denied') {
    throw "Group assignment writes must pass the server-side delegation guard before membership mutation."
}
if ($delegationGuard -notmatch 'scopeAuthorization\.AuthorizeAsync' -or $delegationGuard -notmatch 'groupGrants\.ListAsync' -or $delegationGuard -notmatch 'tenantAuthorization\.AuthorizeAsync') {
    throw "Delegation guard must distinguish identity-scope authority and verify tenant group capabilities against the actor."
}
if ($delegationGuard -notmatch '!grant\.Pattern\.IsConcrete' -or $delegationGuard -notmatch 'grant\.IncludeDescendants') {
    throw "Tenant-scoped delegation must fail closed for wildcard or descendant-expanding group grants."
}
if ($groupGrantReader -notmatch 'managed_group_policy_bindings' -or $groupGrantReader -notmatch 'managed_policy_statements' -or $groupGrantReader -notmatch 'ug\.group_id\s*=\s*@group_id') {
    throw "Group delegation projection must be derived from the selected active tenant group's published managed-policy bindings."
}

# Membership UI must expose current group assignments and a controlled reconciliation workflow.
if ($overviewService -notmatch 'tenantGroupAssignments\.list' -or $overviewService -notmatch 'group-membership",\s*"read"' -or $overviewService -notmatch 'group-membership",\s*"write"') {
    throw "Membership overview must capability-gate tenant group assignment reads and writes."
}
if ($membershipPage -notmatch 'AdminManageMemberGroupsDialog' -or $membershipPage -notmatch 'groupAssignments' -or $membershipPage -notmatch 'isTemplate') {
    throw "Tenant members must display real group assignments and reusable-group state when authorized."
}
if ($manageGroups -notmatch 'Manage groups' -or $manageGroups -notmatch 'groupSelection' -or $manageGroups -notmatch 'TEMPLATE' -or $manageGroups -notmatch 'GROUP') {
    throw "Manage groups must reconcile explicit existing tenant-group selections."
}
if ($mutationService -notmatch 'replaceTenantMemberGroups' -or $mutationService -notmatch 'groups\.list' -or $mutationService -notmatch 'groups\.addMember' -or $mutationService -notmatch 'groups\.removeMember') {
    throw "R4-C group reconciliation must operate only on existing real tenant groups."
}
if ($mutationService -match 'replaceTenantMemberGroups[\s\S]{0,5000}createFromTemplate') {
    throw "Manage groups must not create groups implicitly; Create from template is an explicit Groups workspace action."
}

Write-Host "Member group assignment and safe Add member source consistency validation passed."
