$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root =
    Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Require-Text {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $path = Join-Path $root $RelativePath

    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Pack 6 source '$RelativePath' is missing."
    }

    $source = [System.IO.File]::ReadAllText($path)

    if (-not $source.Contains($Text)) {
        throw "'$RelativePath' is missing required Pack 6 marker '$Text'."
    }
}

Require-Text "src\IdentityAccess.Api\Controllers\OrganisationProfilesController.cs" "OrganisationProfileAdministrationCapabilities.Profiles"
Require-Text "src\IdentityAccess.Api\Controllers\OrganisationProfilesController.cs" "OrganisationProfileTenantBoundary.Matches"
Require-Text "src\IdentityAccess.Api\Controllers\OrganisationProfileDomainOverridesController.cs" "OrganisationProfileAdministrationCapabilities.DomainOverrides"
Require-Text "src\IdentityAccess.Api\Controllers\OrganisationProfileEffectiveVersionsController.cs" "OrganisationProfileAdministrationCapabilities.EffectiveVersions"
Require-Text "src\IdentityAccess.Api\Controllers\OrganisationProfileTemplatesController.cs" "OrganisationProfileAdministrationCapabilities.Templates"
Require-Text "src\IdentityAccess.Api\Controllers\OrganisationProfileTemplateVersionsController.cs" "OrganisationProfileTemplateVersionPublished"
Require-Text "src\IdentityAccess.Api\Http\ApiExceptionHandler.cs" "DomainRegistryVersionUnavailableException"
Require-Text "clients\typescript\src\client\administration\IdentityAccessAdministrationClient.ts" "organisationProfileTemplateVersions"
Require-Text "clients\typescript\src\client\IdentityAccessPathBuilder.ts" "organisationProfilesPath"
Require-Text "clients\typescript\package.json" '"version": "0.26.0"'

$clientRoot =
    Join-Path $root "clients\typescript"

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm was not found on PATH."
}

Push-Location $clientRoot
try {
    npm run typecheck
    if ($LASTEXITCODE -ne 0) {
        throw "OrganisationProfile TypeScript typecheck failed."
    }

    npm test
    if ($LASTEXITCODE -ne 0) {
        throw "OrganisationProfile TypeScript client tests failed."
    }
}
finally {
    Pop-Location
}

Write-Host "OrganisationProfile API + TypeScript SDK validation: GREEN"
