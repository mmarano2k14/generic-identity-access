[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-RequiredFile([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "R4-D final source is incomplete: missing $relativePath."
    }
    return Get-Content -LiteralPath $path -Raw
}

$membershipsPage = Read-RequiredFile "examples/nextjs/admin/app/identity/memberships/page.tsx"
$groupsPage = Read-RequiredFile "examples/nextjs/admin/app/identity/groups/page.tsx"
$policiesPage = Read-RequiredFile "examples/nextjs/admin/app/identity/policies/page.tsx"
$sessionsPage = Read-RequiredFile "examples/nextjs/admin/app/identity/sessions/page.tsx"
$mfaPage = Read-RequiredFile "examples/nextjs/admin/app/identity/mfa/page.tsx"
$loginPage = Read-RequiredFile "examples/nextjs/admin/app/login/page.tsx"
$callbackPage = Read-RequiredFile "examples/nextjs/admin/app/auth/callback/page.tsx"
$adminPackage = Read-RequiredFile "examples/nextjs/admin/package.json"
$runAdminApi = Read-RequiredFile "scripts/authentication/run-dev-admin-api.ps1"
$legacyPolicyDialog = Read-RequiredFile "examples/nextjs/admin/components/AdminCreatePolicyDialog.tsx"
$browserQualification = Read-RequiredFile "scripts/verify-admin-ui-browser-qualification.ps1"
$finalQualification = Read-RequiredFile "scripts/verify-r4-final.ps1"
$administrationSpec = Read-RequiredFile "docs/ADMINISTRATION_TENANTS_MEMBERSHIPS_AND_GROUPS.md"
$e2eDoc = Read-RequiredFile "docs/ADMIN_UI_E2E_QUALIFICATION.md"

foreach ($token in @('AdminMembershipTenantTable', 'memberCountLabel', 'AdminAddTenantMemberDialog', 'AdminManageMemberGroupsDialog', 'isTemplate')) {
    if ($membershipsPage -notmatch [regex]::Escape($token)) {
        throw "R4 final Memberships UI is missing required tenant/member/group contract: $token"
    }
}

foreach ($token in @('groups.listTemplates', 'Create from template', 'Make available as template', 'isTemplate')) {
    if ($groupsPage -notmatch [regex]::Escape($token)) {
        throw "R4 final Groups UI is missing required group-as-template contract: $token"
    }
}
if ($groupsPage -match 'Available group templates|groupTemplates\.') {
    throw "R4 final Groups UI must not restore the retired separate template catalogue."
}

if ($policiesPage -notmatch 'IdentityAccessAdminManagedPolicyReadService' -or $policiesPage -notmatch 'Managed policies' -or $policiesPage -match 'createPolicyAction|permissionPolicies') {
    throw "R4 final Policies workspace must remain managed-policy only."
}

if ($sessionsPage.Length -lt 100 -or $mfaPage.Length -lt 100) {
    throw "R4 final security workspace must retain Sessions and MFA administration pages."
}

if ($loginPage.Length -lt 100 -or $callbackPage.Length -lt 100) {
    throw "R4 final admin host must retain the real login and OIDC callback pages."
}

if ($adminPackage -notmatch 'next dev --hostname 127\.0\.0\.1 --port 3000') {
    throw "The local admin host must use 127.0.0.1:3000 consistently with the registered OIDC callback."
}
foreach ($required in @(
    'http://127.0.0.1:3000/auth/callback',
    'http://127.0.0.1:3000/',
    'http://127.0.0.1:5080'
)) {
    if ($runAdminApi -notmatch [regex]::Escape($required)) {
        throw "The local administration API launcher is missing the pinned loopback endpoint: $required"
    }
}

if ($legacyPolicyDialog -notmatch 'export\s*\{\s*\}\s*;' -or $legacyPolicyDialog -match 'createPolicyAction|AdminMutationDialog|function\s+AdminCreatePolicyDialog') {
    throw "The retired legacy policy creation dialog must remain an inert tombstone."
}

foreach ($token in @('TenantMembership', 'Create from template', 'managed-policy bindings', 'never copies memberships', 'Resource scope')) {
    if ($administrationSpec -notmatch [regex]::Escape($token)) {
        throw "The administration specification is missing required rule: $token"
    }
}

foreach ($token in @('OIDC sign-in', 'empty tenant', 'Template Yes/No', 'Create from template', 'Add member', 'Manage groups', 'Sessions', 'MFA')) {
    if ($e2eDoc -notmatch [regex]::Escape($token)) {
        throw "The R4 browser qualification runbook is missing required evidence: $token"
    }
}

if ($browserQualification -notmatch 'admin-ui-e2e' -or $browserQualification -notmatch 'Invoke-WebRequest' -or $browserQualification -notmatch 'Read-Host') {
    throw "The R4 browser qualification script must prove reachability and collect explicit human UI evidence."
}

if ($finalQualification -notmatch 'verify-production-qualification\.ps1' -or $finalQualification -notmatch 'verify-admin-ui-browser-qualification\.ps1') {
    throw "The R4 final qualification wrapper must compose production qualification and browser evidence."
}

Write-Host "R4 final administration source consistency validation passed."
