[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required tenant-membership administration source file is missing: $relativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

$page = Read-Source "examples/nextjs/admin/app/identity/memberships/page.tsx"
$readService = Read-Source "examples/nextjs/admin/server/IdentityAccessAdminMembershipOverviewService.ts"
$table = Read-Source "examples/nextjs/admin/components/AdminTenantMembershipOverview.tsx"

if ($page -match 'Inspect tenant membership' -or $page -match 'name="userId"[^\r\n]*kind="user"') {
    throw "Membership administration must not regress to the technical tenant/user lookup workflow."
}

if ($page -notmatch 'Create tenant' -or $page -notmatch 'overview\.scopeWide') {
    throw "Tenant-centric membership administration must keep empty-tenant creation limited to scope-wide administration."
}

if ($readService -notmatch 'tenantVisibility === "scope-wide"' -or
    $readService -notmatch 'activeTenantMemberships' -or
    $readService -notmatch 'memberships\.findByUser\s*\(') {
    throw "Membership overview reads must distinguish scope-wide tenant collections from membership-limited self context."
}

if ($readService -notmatch 'tenantUsers\.list\s*\(' -or
    $readService -notmatch 'memberships\.list\s*\(') {
    throw "Scope-wide membership overview must stay backed by tenant-constrained user and membership reads."
}

if ($table -notmatch 'memberCountLabel' -or
    $table -notmatch 'selfOnly' -or
    $page -notmatch '"Own membership"') {
    throw "Tenant membership presentation must expose member counts without pretending membership-limited subjects can enumerate tenant directories."
}

if ($page -notmatch 'Membership is not permission') {
    throw "Tenant membership administration must preserve the membership-versus-permission boundary."
}

Write-Host "Tenant-centric membership UI source consistency validation passed."
