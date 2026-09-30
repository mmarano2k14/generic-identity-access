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
        throw "Required Pack 8 source '$RelativePath' is missing."
    }

    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Text)) {
        throw "'$RelativePath' is missing required Pack 8 marker '$Text'."
    }
}

Require-Text "IdentityAccess.sln" "OrganisationProfile.QualificationProbe"
Require-Text "tests\OrganisationProfile.QualificationProbe\OrganisationProfile.QualificationProbe.csproj" "OrganisationProfile.Infrastructure.PostgreSql"
Require-Text "tests\OrganisationProfile.QualificationProbe\Program.cs" "Concurrent override writers did not produce exactly one success and one stale-write rejection."
Require-Text "tests\OrganisationProfile.QualificationProbe\Program.cs" "Concurrent identical snapshot publication created duplicate semantic versions."
Require-Text "tests\OrganisationProfile.QualificationProbe\Program.cs" "Equivalent effective content did not reproduce the original content hash."
Require-Text "tests\OrganisationProfile.QualificationProbe\Program.cs" "Cross-tenant Organization reference was unexpectedly accepted."
Require-Text "src\OrganisationProfile.Infrastructure.PostgreSql\PostgreSqlOrganisationProfileVersionWriter.cs" "FOR UPDATE;"
Require-Text "src\OrganisationProfile.Infrastructure.PostgreSql\PostgreSqlOrganisationProfileVersionWriter.cs" "latest.ContentHash == contentHash"
Require-Text "src\OrganisationProfile.Infrastructure.PostgreSql\PostgreSqlOrganisationProfileDomainOverrideStore.cs" "FOR UPDATE;"
Require-Text "src\OrganisationProfile.Infrastructure.PostgreSql\Migrations\0003_effective_composition.sql" "ck_organisation_profile_version_immutable"
Require-Text "src\OrganisationProfile.Infrastructure.PostgreSql\Migrations\0003_effective_composition.sql" "ck_organisation_profile_version_domain_immutable"
Require-Text "src\OrganisationProfile.Application\Composition\OrganisationProfileEffectiveContentHasher.cs" "organisation-profile-effective-content/v1"
Require-Text "src\OrganisationProfile.Application\Templates\OrganisationProfileTemplateContentHasher.cs" "organisation-profile-template-content/v1"
Require-Text "scripts\organisation-profile\postgresql\verify-backup-restore.ps1" "organisation_profile"
Require-Text "docs\organisation-profile\PACK_08_QUALIFICATION_AND_HARDENING.md" "release-candidate"

$migrationRoot = Join-Path $root "src\OrganisationProfile.Infrastructure.PostgreSql\Migrations"
$migrations = @(Get-ChildItem -LiteralPath $migrationRoot -Filter "*.sql" | Sort-Object Name)
$expected = @(
    "0001_organisation_profiles.sql",
    "0002_template_catalog.sql",
    "0003_effective_composition.sql"
)

if ($migrations.Count -ne $expected.Count) {
    throw "Pack 8 must not introduce a PostgreSQL migration. Expected $($expected.Count), found $($migrations.Count)."
}

for ($index = 0; $index -lt $expected.Count; $index++) {
    if ($migrations[$index].Name -ne $expected[$index]) {
        throw "Unexpected OrganisationProfile migration set. Expected '$($expected[$index])', found '$($migrations[$index].Name)'."
    }
}

Write-Host "OrganisationProfile Pack 8 source hardening validation: GREEN"
