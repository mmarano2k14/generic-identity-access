[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Shared Identity SDK Inventory and Category Contract required file is missing: $RelativePath"
    }
    return $path
}

$categoryPath = Require-File 'docs\shared-identity\FULL_SDK_CATEGORY_MODEL.md'
$matrixPath = Require-File 'docs\shared-identity\FULL_SDK_FEATURE_MATRIX.md'
$jsonPath = Require-File 'docs\shared-identity\full-sdk-feature-matrix.json'
$inventoryDocumentPath = Require-File 'docs\shared-identity\FULL_SDK_INVENTORY.md'
$manifestPath = Require-File 'docs\shared-identity\validation-manifests\SDK_INVENTORY.md'

$requiredCategoryLabels = @(
    'Account & Authentication',
    'Directory',
    'Organizations',
    'Access Control',
    'Application Security',
    'Security Operations',
    'Protocol & Diagnostics'
)

$matrixText = [System.IO.File]::ReadAllText($matrixPath)
foreach ($label in $requiredCategoryLabels) {
    if ($matrixText -notmatch [System.Text.RegularExpressions.Regex]::Escape("## $label")) {
        throw "Shared Identity SDK Inventory and Category Contract matrix is missing category '$label'."
    }
}

$matrix = Get-Content $jsonPath -Raw | ConvertFrom-Json
if ($matrix.schemaVersion -ne '1') {
    throw 'Shared Identity SDK Inventory and Category Contract matrix schemaVersion must be 1.'
}
if ($matrix.scope -ne 'generic-identity') {
    throw 'Shared Identity SDK Inventory and Category Contract matrix scope must remain generic-identity.'
}
if (@($matrix.categories).Count -ne 7) {
    throw 'Shared Identity SDK Inventory and Category Contract matrix must contain exactly seven frozen categories.'
}

$requiredCategoryKeys = @(
    'account-authentication',
    'directory',
    'organizations',
    'access-control',
    'application-security',
    'security-operations',
    'protocol-diagnostics'
)
foreach ($key in $requiredCategoryKeys) {
    $match = @($matrix.categories | Where-Object { $_.key -eq $key })
    if ($match.Count -ne 1) {
        throw "Shared Identity SDK Inventory and Category Contract matrix must contain category '$key' exactly once."
    }
    if (@($match[0].features).Count -lt 1) {
        throw "Shared Identity SDK Inventory and Category Contract category '$key' must contain at least one feature."
    }
}

$allowedStatuses = @(
    'COMPLETE',
    'COMPLETE_READ',
    'SDK_PARTIAL',
    'SDK_MISSING',
    'SDK_UI_MISSING',
    'UI_MISSING',
    'NEXT_FLOW_MISSING',
    'BACKEND_MISSING',
    'BACKEND_API_MISSING',
    'BACKEND_CONTRACT_MISSING',
    'SDK_CONTRACT_FREEZE_REQUIRED',
    'SDK_OPTIONAL',
    'RETIRED_COMPATIBILITY'
)
foreach ($category in $matrix.categories) {
    foreach ($feature in $category.features) {
        if ($allowedStatuses -notcontains [string]$feature.status) {
            throw "Shared Identity SDK Inventory and Category Contract feature '$($feature.key)' has unknown status '$($feature.status)'."
        }
        foreach ($property in @('key','label','backend','legacyClient','publicSdk','sharedUi','status')) {
            $value = $feature.PSObject.Properties[$property]
            if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value.Value)) {
                throw "Shared Identity SDK Inventory and Category Contract feature '$($feature.key)' is missing '$property'."
            }
        }
    }
}

# Anchor the inventory to representative current backend/API boundaries.
$representativeBackendFiles = @(
    'src\IdentityAccess.Api\Controllers\AuthenticationController.cs',
    'src\IdentityAccess.Api\Controllers\UsersController.cs',
    'src\IdentityAccess.Api\Controllers\TenantsController.cs',
    'src\IdentityAccess.Api\Controllers\TenantMembershipsController.cs',
    'src\IdentityAccess.Api\Controllers\GroupsController.cs',
    'src\IdentityAccess.Api\Controllers\ManagedPoliciesController.cs',
    'src\IdentityAccess.Api\Controllers\ResourceScopesController.cs',
    'src\IdentityAccess.Api\Controllers\ApplicationSecurityModelsController.cs',
    'src\IdentityAccess.Api\Controllers\OrganizationsController.cs',
    'src\IdentityAccess.Api\Controllers\MfaAdministrationController.cs',
    'src\IdentityAccess.Api\Controllers\SecurityAuditEventsController.cs',
    'src\IdentityAccess.Api\Controllers\OidcAuthorizationController.cs'
)
foreach ($file in $representativeBackendFiles) { [void](Require-File $file) }

# Anchor the inventory to the proven legacy TypeScript client and current public package boundaries.
$representativeClientFiles = @(
    'clients\typescript\src\client\IdentityAccessAuthenticationClient.ts',
    'clients\typescript\src\client\IdentityAccessAuthorizationClient.ts',
    'clients\typescript\src\client\IdentityAccessOidcClient.ts',
    'clients\typescript\src\client\administration\IdentityAccessAdministrationClient.ts',
    'packages\auth\src\administration.ts',
    'packages\auth\src\authentication.ts',
    'packages\auth\src\authorization.ts',
    'packages\react\src\pages\UsersPage.tsx',
    'packages\react\src\pages\GroupsPage.tsx',
    'packages\react\src\pages\MfaPage.tsx'
)
foreach ($file in $representativeClientFiles) { [void](Require-File $file) }

# OrganisationProfile is deliberately outside the categorized Generic Identity SDK.
$categoryText = [System.IO.File]::ReadAllText($categoryPath)
if ($categoryText -notmatch 'OrganisationProfile.*not.*promoted') {
    throw 'Shared Identity SDK Inventory and Category Contract must keep OrganisationProfile outside the categorized Generic Identity SDK.'
}

# SDK Inventory and Category Contract itself must remain consumer-neutral. The public/runtime source tree is
# anchored above; consumer applications supply their own ApplicationKey/ClientId values.
$inventoryFiles = @(
    $categoryPath,
    $matrixPath,
    $jsonPath,
    $inventoryDocumentPath,
    $manifestPath
)
foreach ($file in $inventoryFiles) {
    $text = [System.IO.File]::ReadAllText($file)
    if ($text -match '(?i)consumer-specific-product-name') {
        throw "Shared Identity SDK Inventory and Category Contract contains a consumer-specific naming placeholder: $file"
    }
}

$manifestText = [System.IO.File]::ReadAllText($manifestPath)
foreach ($marker in @('## MOVED', 'NONE', '## DELETED', '## DATABASE MIGRATIONS', '## BREAKING CHANGES')) {
    if ($manifestText -notmatch [System.Text.RegularExpressions.Regex]::Escape($marker)) {
        throw "Shared Identity SDK Inventory and Category Contract manifest is missing marker '$marker'."
    }
}

Write-Host 'Shared Identity SDK Inventory and Category Contract full SDK inventory/category validation: GREEN'
