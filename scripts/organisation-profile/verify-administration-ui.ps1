$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Require-Text {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Pack 7 source '$RelativePath' is missing."
    }

    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Text)) {
        throw "'$RelativePath' is missing required Pack 7 marker '$Text'."
    }
}

Require-Text "examples\nextjs\admin\app\organisations\[organizationId]\profile\page.tsx" "OrganisationProfileWorkspaceService"
Require-Text "examples\nextjs\admin\components\organisation-profile\OrganisationProfilePanel.tsx" "ProfileTemplateSelector"
Require-Text "examples\nextjs\admin\components\organisation-profile\OrganisationProfilePanel.tsx" "DomainCompositionPanel"
Require-Text "examples\nextjs\admin\components\organisation-profile\OrganisationProfilePanel.tsx" "ProfileVersionPanel"
Require-Text "examples\nextjs\admin\components\organisation-profile\OrganisationProfilePanel.tsx" "ProfileLifecycleActions"
Require-Text "examples\nextjs\admin\server\OrganisationProfileWorkspaceService.ts" "organisationProfileTemplates.list"
Require-Text "examples\nextjs\admin\server\OrganisationProfileMutationService.ts" "active published catalog reference"

$navigationPath = Join-Path $root "examples\nextjs\admin\components\AdminNavigation.tsx"
$navigation = [System.IO.File]::ReadAllText($navigationPath)
if ($navigation -imatch [System.Text.RegularExpressions.Regex]::Escape("organisation-profile")) {
    throw "OrganisationProfile must not become an Identity Access navigation section."
}

$mutationPath = Join-Path $root "examples\nextjs\admin\server\OrganisationProfileMutationService.ts"
$mutation = [System.IO.File]::ReadAllText($mutationPath)
foreach ($forbidden in @(
    "administration.organizations.create",
    "administration.organizations.update",
    "administration.organizations.enable",
    "administration.organizations.disable"
)) {
    if ($mutation.Contains($forbidden)) {
        throw "OrganisationProfile UI must not mutate Organization identity: '$forbidden'."
    }
}

$hostRoot = Join-Path $root "examples\nextjs\admin"
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm was not found on PATH."
}

Push-Location $hostRoot
try {
    npm run verify
    if ($LASTEXITCODE -ne 0) {
        throw "OrganisationProfile administration UI verification failed."
    }
}
finally {
    Pop-Location
}

Write-Host "OrganisationProfile administration UI validation: GREEN"
