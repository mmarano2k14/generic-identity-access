[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Access Control required file is missing: $RelativePath"
    }
    return $path
}

function Read-Source([string]$RelativePath) {
    return [System.IO.File]::ReadAllText((Require-File $RelativePath))
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    if (-not (Read-Source $RelativePath).Contains($Needle)) {
        throw "Access Control file '$RelativePath' is missing marker '$Needle'."
    }
}

function Reject-Text([string]$RelativePath, [string]$Needle) {
    if ((Read-Source $RelativePath).Contains($Needle)) {
        throw "Access Control file '$RelativePath' contains forbidden marker '$Needle'."
    }
}

$categoryFiles = @(
    'packages/contracts/src/access-control/index.ts',
    'packages/contracts/test/consumer-access-control.ts',
    'packages/auth/src/access-control.ts',
    'packages/auth/test/consumer-access-control.ts',
    'packages/react/src/access-control/index.ts',
    'packages/react/src/access-control/GroupForm.tsx',
    'packages/react/src/access-control/GroupMemberForm.tsx',
    'packages/react/src/access-control/ManagedPolicyForm.tsx',
    'packages/react/src/access-control/ManagedPolicyVersionForm.tsx',
    'packages/react/src/access-control/ManagedPolicyStatementForm.tsx',
    'packages/react/src/access-control/ManagedPolicyBindingForm.tsx',
    'packages/react/src/access-control/ResourceScopeForm.tsx',
    'packages/react/src/access-control/DelegatedAuthorityGroupForm.tsx',
    'packages/react/src/access-control/DelegatedAuthorityMemberForm.tsx',
    'packages/react/src/pages/GroupAccessPage.tsx',
    'packages/react/src/pages/ManagedPolicyBindingsPage.tsx',
    'packages/react/src/pages/ResourceScopesPage.tsx',
    'packages/react/src/pages/DelegatedAuthorityPage.tsx',
    'packages/react/test/consumer-access-control.tsx',
    'packages/next/src/access-control/index.ts',
    'packages/next/test/consumer-access-control.tsx',
    'docs/shared-identity/ACCESS_CONTROL.md',
    'docs/shared-identity/validation-manifests/ACCESS_CONTROL.md'
)

foreach ($file in $categoryFiles) {
    [void](Require-File $file)
}

# Preserve the managed-policy compatibility closure. The retired legacy policies
# client/file may remain in source history, but must not be composed or exported.
$administrationClient = 'clients/typescript/src/client/administration/IdentityAccessAdministrationClient.ts'
Reject-Text $administrationClient 'IdentityAccessPoliciesClient'
Reject-Text $administrationClient 'public readonly policies:'

Reject-Text 'clients/typescript/src/index.ts' 'IdentityAddGroupPolicyBindingRequest'
Reject-Text 'clients/typescript/src/index.ts' 'IdentityGroupPolicyBindingRecord'

Require-Text 'packages/auth/src/client.ts' 'readonly accessControl: GenericIdentityAccessControlClient;'
Reject-Text 'packages/auth/src/client.ts' 'tenantPolicies:'
Require-Text 'packages/auth/src/client.ts' 'managedPolicies: legacy.administration.managedPolicies'
Require-Text 'packages/auth/src/client.ts' 'managedPolicyBindings: legacy.administration.managedPolicyBindings'
Require-Text 'packages/auth/src/client.ts' 'delegatedAuthority: legacy.administration.scopeAuthority'
Require-Text 'packages/auth/src/client.ts' 'authorization: legacy.authorization'

Require-Text 'packages/auth/src/access-control.ts' 'GenericIdentityAccessGroupsClient'
Reject-Text 'packages/auth/src/access-control.ts' 'GenericIdentityTenantPoliciesClient'
Require-Text 'packages/auth/src/access-control.ts' 'GenericIdentityManagedPoliciesCatalogClient'
Require-Text 'packages/auth/src/access-control.ts' 'GenericIdentityManagedPolicyBindingsClient'
Require-Text 'packages/auth/src/access-control.ts' 'GenericIdentityResourceScopesClient'
Require-Text 'packages/auth/src/access-control.ts' 'GenericIdentityDelegatedAuthorityClient'
Require-Text 'packages/auth/src/access-control.ts' 'IdentityCreatePolicyRequest'
Require-Text 'packages/auth/src/access-control.ts' 'IdentityUpdatePolicyRequest'
Require-Text 'packages/auth/src/access-control.ts' 'IdentityAddPolicyStatementRequest'

Require-Text 'packages/contracts/package.json' '"./access-control"'
Require-Text 'packages/auth/package.json' '"./access-control"'
Require-Text 'packages/react/package.json' '"./access-control"'
Require-Text 'packages/next/package.json' '"./access-control"'

Require-Text 'packages/react/src/access-control/index.ts' 'ResourceScopeForm'
Require-Text 'packages/react/src/access-control/index.ts' 'DelegatedAuthorityPage'
Reject-Text 'packages/react/src/access-control/index.ts' 'export * from "../pages/TenantPoliciesPage";'
Reject-Text 'packages/react/src/access-control/index.ts' 'export * from "../pages/TenantPolicyDetailsPage";'
Reject-Text 'packages/react/src/access-control/index.ts' 'export * from "./TenantPolicyForm";'
Reject-Text 'packages/react/src/access-control/index.ts' 'export * from "./PolicyStatementForm";'
Reject-Text 'packages/react/src/access-control/index.ts' 'export * from "./PolicyBindingForm";'
Require-Text 'packages/react/src/access-control/index.ts' 'export * from "./ManagedPolicyBindingForm";'
Reject-Text 'packages/next/src/pages/index.ts' 'TenantPoliciesPage'
Reject-Text 'packages/next/src/pages/index.ts' 'TenantPolicyDetailsPage'

foreach ($tombstone in @(
    'packages/react/src/access-control/TenantPolicyForm.tsx',
    'packages/react/src/access-control/PolicyStatementForm.tsx',
    'packages/react/src/access-control/PolicyBindingForm.tsx',
    'packages/react/src/pages/TenantPoliciesPage.tsx',
    'packages/react/src/pages/TenantPolicyDetailsPage.tsx'
)) {
    Require-Text $tombstone 'export {};'
}

Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'RETIRED_COMPATIBILITY'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'Effective permission listing'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'BACKEND_MISSING'

$versions = @{}
foreach ($packageName in @('contracts', 'auth', 'react', 'next')) {
    $manifestPath = Join-Path $root "packages/$packageName/package.json"
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $version = [version][string]$manifest.version
    $versions[$packageName] = $version

    if ($version -lt [version]'1.3.0') {
        throw "Access Control package '$packageName' must remain at or above version 1.3.0."
    }
}

$alignedVersions = @(
    $versions.Values |
        ForEach-Object { $_.ToString() } |
        Sort-Object -Unique
)

if ($alignedVersions.Count -ne 1) {
    throw 'Access Control Generic Identity public package versions must remain aligned.'
}

foreach ($file in $categoryFiles) {
    $source = [System.IO.File]::ReadAllText((Join-Path $root $file))
    if ($source.IndexOf('consumer-app', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Access Control consumer-neutral source '$file' must not contain a consumer product name."
    }
}

Write-Host 'Shared Identity Access Control SDK/UI source validation: GREEN'
