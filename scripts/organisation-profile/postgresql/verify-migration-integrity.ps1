$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

. (Join-Path $PSScriptRoot "common.ps1")

$root =
    Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

$migrationRoot =
    Join-Path $root "src\OrganisationProfile.Infrastructure.PostgreSql\Migrations"

$migrations =
    Get-ChildItem $migrationRoot -Filter "*.sql" |
    Sort-Object Name

if (-not $migrations) {
    throw "No OrganisationProfile PostgreSQL migrations were found."
}

$metadataTable =
    Invoke-OrganisationProfilePsqlText `
        -Sql "SELECT CASE WHEN to_regclass('organisation_profile.schema_migrations') IS NULL THEN '0' ELSE '1' END;"

if ($metadataTable -ne "1") {
    throw "organisation_profile.schema_migrations is missing. Apply the schema first."
}

$knownVersions =
    New-Object System.Collections.Generic.List[int]

foreach ($migration in $migrations) {
    if ($migration.Name -notmatch '^(\d{4})_.*\.sql$') {
        throw "Invalid OrganisationProfile migration filename '$($migration.Name)'."
    }

    $version = [int]$Matches[1]
    $knownVersions.Add($version)

    $checksum =
        Get-OrganisationProfileMigrationChecksum `
            -Path $migration.FullName

    $metadata =
        Invoke-OrganisationProfilePsqlText `
            -Sql "SELECT name, checksum FROM organisation_profile.schema_migrations WHERE version = $version;"

    if (-not $metadata) {
        throw "OrganisationProfile migration '$($migration.Name)' is not applied."
    }

    $parts = $metadata -split '\|', 2

    if ($parts[0] -ne $migration.Name) {
        throw "OrganisationProfile migration $version filename mismatch."
    }

    if ($parts.Count -lt 2 -or
        $parts[1].Trim() -ne $checksum) {
        throw "OrganisationProfile migration '$($migration.Name)' checksum mismatch."
    }
}

$knownVersionList =
    ($knownVersions | ForEach-Object { $_.ToString() }) -join ","

$unknown =
    Invoke-OrganisationProfilePsqlText `
        -Sql "SELECT COALESCE(string_agg(version::text, ',' ORDER BY version), '') FROM organisation_profile.schema_migrations WHERE version NOT IN ($knownVersionList);"

if ($unknown) {
    throw "OrganisationProfile migration integrity failed: unknown applied versions [$unknown]."
}

Write-Host "OrganisationProfile migration integrity: GREEN"
