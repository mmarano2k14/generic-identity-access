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

$organizationTable =
    Invoke-OrganisationProfilePsqlText `
        -Sql "SELECT CASE WHEN to_regclass('organization_directory.organizations') IS NULL THEN '0' ELSE '1' END;"

if ($organizationTable -ne "1") {
    throw "Organization Directory schema is not initialized. Apply Organization Directory migrations first."
}

$settings =
    Get-OrganisationProfilePostgresSettings

Write-Host "Applying organisation_profile schema to '$($settings.Database)'..."

$metadataSql = @"
CREATE SCHEMA IF NOT EXISTS organisation_profile;

CREATE TABLE IF NOT EXISTS organisation_profile.schema_migrations
(
    version integer PRIMARY KEY,
    name varchar(200) NOT NULL,
    checksum char(64) NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT transaction_timestamp()
);
"@

Invoke-OrganisationProfilePsqlText -Sql $metadataSql | Out-Null

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

    if ($metadata) {
        $parts = $metadata -split '\|', 2

        if ($parts[0] -ne $migration.Name) {
            throw "OrganisationProfile migration $version filename mismatch."
        }

        if ($parts.Count -lt 2 -or
            $parts[1].Trim() -ne $checksum) {
            throw "OrganisationProfile migration '$($migration.Name)' checksum mismatch."
        }

        Write-Host "Skipping $($migration.Name) (already applied and verified)."
        continue
    }

    Write-Host "Applying $($migration.Name)..."

    Invoke-OrganisationProfilePsqlFile `
        -Path $migration.FullName `
        -SingleTransaction

    $escapedName =
        $migration.Name.Replace("'", "''")

    Invoke-OrganisationProfilePsqlText `
        -Sql "INSERT INTO organisation_profile.schema_migrations(version, name, checksum) VALUES ($version, '$escapedName', '$checksum');" |
        Out-Null
}

$knownVersionList =
    ($knownVersions | ForEach-Object { $_.ToString() }) -join ","

$unknown =
    Invoke-OrganisationProfilePsqlText `
        -Sql "SELECT COALESCE(string_agg(version::text, ',' ORDER BY version), '') FROM organisation_profile.schema_migrations WHERE version NOT IN ($knownVersionList);"

if ($unknown) {
    throw "OrganisationProfile migration integrity failed: unknown applied versions [$unknown]."
}

Write-Host "OrganisationProfile PostgreSQL schema apply: GREEN"
