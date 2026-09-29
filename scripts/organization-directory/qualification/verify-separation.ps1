$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

function Read-Source {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path)) {
        throw "Required Organization Directory source '$RelativePath' was not found."
    }

    return [System.IO.File]::ReadAllText($path)
}

function Require-Text {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $source = Read-Source $RelativePath
    if (-not $source.Contains($Text)) {
        throw "'$RelativePath' is missing required separation marker '$Text'."
    }
}

function Forbid-Text {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Text
    )

    $source = Read-Source $RelativePath
    if ($source.Contains($Text)) {
        throw "'$RelativePath' violates Organization Directory separation with '$Text'."
    }
}

function Require-MaximumLines {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][int]$Maximum
    )

    $path = Join-Path $root $RelativePath
    $count = [System.IO.File]::ReadAllLines($path).Length

    if ($count -gt $Maximum) {
        throw "'$RelativePath' has $count lines and exceeds the focused-component limit of $Maximum."
    }
}

$genericMutation = "examples\nextjs\admin\server\IdentityAccessAdminMutationService.ts"
$organizationMutation = "examples\nextjs\admin\server\IdentityAccessAdminOrganizationMutationService.ts"
$membershipMutation = "examples\nextjs\admin\server\IdentityAccessAdminOrganizationMembershipMutationService.ts"
$scopeLinkMutation = "examples\nextjs\admin\server\IdentityAccessAdminOrganizationScopeLinkMutationService.ts"
$membershipOverview = "examples\nextjs\admin\server\IdentityAccessAdminMembershipOverviewService.ts"
$organizationOverview = "examples\nextjs\admin\server\IdentityAccessAdminOrganizationOverviewService.ts"
$organizationPanel = "examples\nextjs\admin\components\AdminOrganizationDirectoryPanel.tsx"
$organizationTable = "examples\nextjs\admin\components\AdminOrganizationTable.tsx"
$scopePanel = "examples\nextjs\admin\components\AdminOrganizationScopeLinkPanel.tsx"

Forbid-Text $genericMutation ".administration.organizations"
Forbid-Text $genericMutation ".administration.organizationMemberships"
Forbid-Text $genericMutation ".administration.organizationResourceScopeLinks"

Require-Text $organizationMutation ".administration.organizations"
Forbid-Text $organizationMutation ".administration.organizationMemberships"
Forbid-Text $organizationMutation ".administration.organizationResourceScopeLinks"

Require-Text $membershipMutation ".administration.organizationMemberships"
Forbid-Text $membershipMutation ".administration.organizationResourceScopeLinks"

Require-Text $scopeLinkMutation ".administration.organizationResourceScopeLinks"
Forbid-Text $scopeLinkMutation ".administration.organizationMemberships"

Forbid-Text $membershipOverview ".administration.organizations"
Forbid-Text $membershipOverview ".administration.organizationMemberships"
Forbid-Text $membershipOverview "organization-scope-link"

Require-Text $organizationOverview ".administration.organizations"
Require-Text $organizationOverview ".administration.organizationMemberships"
Require-Text $organizationOverview "organization-scope-link"
Forbid-Text $organizationOverview ".administration.groups"
Forbid-Text $organizationOverview ".administration.tenantGroupAssignments"

Require-Text $organizationPanel "AdminOrganizationCreateDialog"
Require-Text $organizationPanel "AdminOrganizationTable"
Require-Text $organizationPanel "AdminOrganizationScopeLinkPanel"
Forbid-Text $organizationPanel "AdminMutationDialog"
Forbid-Text $organizationPanel "../app/identity/actions"

Require-Text $organizationTable "AdminOrganizationRowActions"
Forbid-Text $organizationTable "AdminMutationDialog"

Require-Text $scopePanel "AdminEntityAutocomplete"
Require-Text $scopePanel 'name="resourceScopeId"'
Forbid-Text $scopePanel 'AdminSelectField label="Resource scope"'

$separatePage = Join-Path $root "examples\nextjs\admin\app\identity\organizations\page.tsx"
if (Test-Path $separatePage) {
    throw "Organization Directory must remain embedded in Identity Membership; a separate Organization page was found."
}

Require-MaximumLines $organizationMutation 230
Require-MaximumLines $membershipMutation 220
Require-MaximumLines $scopeLinkMutation 180
Require-MaximumLines $organizationOverview 220
Require-MaximumLines $organizationPanel 140

Write-Host "Organization Directory separation validation: GREEN"
