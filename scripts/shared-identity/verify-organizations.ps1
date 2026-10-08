[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Organizations required file is missing: $RelativePath" }
    return $path
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $text = [System.IO.File]::ReadAllText($path)
    if (-not $text.Contains($Needle)) { throw "Organizations file '$RelativePath' is missing marker '$Needle'." }
}

$categoryFiles = @(
    'packages/contracts/src/organizations/index.ts',
    'packages/auth/src/organizations.ts',
    'packages/react/src/organizations/index.ts',
    'packages/react/src/organizations/OrganizationForm.tsx',
    'packages/react/src/organizations/OrganizationMembershipForm.tsx',
    'packages/react/src/organizations/OrganizationResourceScopeLinkForm.tsx',
    'packages/react/src/pages/OrganizationsPage.tsx',
    'packages/react/src/pages/OrganizationDetailsPage.tsx',
    'packages/react/src/pages/OrganizationMembershipsPage.tsx',
    'packages/react/src/pages/OrganizationTreePage.tsx',
    'packages/next/src/organizations/index.ts',
    'docs/shared-identity/ORGANIZATIONS.md',
    'docs/shared-identity/validation-manifests/ORGANIZATIONS.md'
)

foreach ($file in $categoryFiles) { Require-File $file | Out-Null }

Require-Text 'packages/auth/src/client.ts' 'readonly organizations: GenericIdentityOrganizationsClient;'
Require-Text 'packages/auth/src/client.ts' 'organizations: legacy.administration.organizations'
Require-Text 'packages/auth/src/organizations.ts' 'GenericIdentityOrganizationRecordsClient'
Require-Text 'packages/auth/src/organizations.ts' 'listForTenantMembership'
Require-Text 'packages/auth/src/organizations.ts' 'GenericIdentityOrganizationResourceScopeLinksClient'
Require-Text 'packages/react/src/organizations/index.ts' 'OrganizationResourceScopeLinkForm'
Require-Text 'packages/react/src/pages/index.ts' 'OrganizationTreePage'
Require-Text 'packages/next/src/index.ts' './organizations/index'
Require-Text 'packages/next/src/pages/index.ts' '@generic-identity/react/organizations'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'GenericIdentityOrganizationsClient.organizations full CRUD + tree + lifecycle'

$versions = @{}
foreach ($packageName in @('contracts', 'auth', 'react', 'next')) {
    $manifest = Get-Content (Join-Path $root "packages/$packageName/package.json") -Raw | ConvertFrom-Json
    $versions[$packageName] = [version][string]$manifest.version
    if ($versions[$packageName] -lt [version]'1.2.0') { throw "Organizations package '$packageName' must remain at or above version 1.2.0." }
}
$alignedVersions = @(
    $versions.Values |
        ForEach-Object { $_.ToString() } |
        Sort-Object -Unique
)

if ($alignedVersions.Count -ne 1) {
    throw 'Organizations Generic Identity public package versions must remain aligned.'
}



Require-Text 'packages/next/src/server/organization-workspace.ts' 'loadNextOrganizationWorkspace'
Require-Text 'packages/next/src/server/organization-workspace.ts' 'organization-scope-link'
Require-Text 'packages/next/src/server/organization-mutations.ts' 'createNextOrganizationFromForm'
Require-Text 'packages/next/src/server/organization-mutations.ts' 'linkNextOrganizationResourceScopeFromForm'
Require-Text 'packages/next/src/server/index.ts' 'loadNextOrganizationWorkspace'
Require-Text 'packages/react/src/organizations/OrganizationForm.tsx' 'typeof formAction === "function"'
Require-Text 'packages/react/src/organizations/OrganizationMembershipForm.tsx' 'kind="tenant-membership"'
Require-Text 'packages/react/src/organizations/OrganizationResourceScopeLinkForm.tsx' 'kind="resource-scope"'
Require-Text 'packages/react/src/pages/OrganizationsPage.tsx' 'hierarchyLabel'
# ResourceScope autocomplete remains ACTIVE-only by default for Organization links.
# Resource Scope hierarchy editing may explicitly opt into inactive references.
Require-Text 'packages/next/src/server/entity-references.ts' 'includeInactive = false'
Require-Text 'packages/next/src/server/entity-references.ts' '.filter((record) => includeInactive || record.status === 1)'

& node (Join-Path $root 'packages/next/test/organization-workflows.behavior.test.mjs')
if ($LASTEXITCODE -ne 0) { throw 'Organizations parity behavior tests failed.' }
Write-Host 'Shared Identity Organizations parity behavior tests: GREEN'

Write-Host 'Shared Identity Organizations SDK/UI source validation: GREEN'
