[CmdletBinding()]
param(
    [string]$MaintenanceDatabase = "postgres",
    [string]$ArtifactPath,
    [switch]$KeepArtifact
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

foreach ($commandName in @("psql", "pg_dump", "pg_restore")) {
    if (-not (Get-Command $commandName -ErrorAction SilentlyContinue)) {
        throw "$commandName was not found on PATH."
    }
}

. (Join-Path $PSScriptRoot "common.ps1")

$settings = Get-OrganisationProfilePostgresSettings
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

if ([string]::IsNullOrWhiteSpace($ArtifactPath)) {
    $fileName = "organisation-profile-restore-qualification-{0}.dump" -f (Get-Date -Format "yyyyMMdd-HHmmss")
    $ArtifactPath = Join-Path ([System.IO.Path]::GetTempPath()) $fileName
}

$ArtifactPath = [System.IO.Path]::GetFullPath($ArtifactPath)
$targetDatabase = "organisation_profile_restore_{0}_{1}" -f (
    (Get-Date -Format "yyyyMMddHHmmss"),
    ([Guid]::NewGuid().ToString("N").Substring(0, 8))
)

$targetCreated = $false
$previousPassword = $env:PGPASSWORD
$previousDatabaseOverride = $env:ORGANISATION_PROFILE_POSTGRES_DATABASE

function Invoke-QualificationPsql {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Database,

        [Parameter(Mandatory = $true)]
        [string]$Sql
    )

    $output = & psql `
        -h $settings.Host `
        -p $settings.Port `
        -U $settings.User `
        -d $Database `
        -v ON_ERROR_STOP=1 `
        -At `
        -F "|" `
        -c $Sql

    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL qualification command failed against '$Database'."
    }

    if ($null -eq $output) {
        return ""
    }

    return ($output | Out-String).Trim()
}

function Get-QualificationCounts {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Database
    )

    return Invoke-QualificationPsql `
        -Database $Database `
        -Sql @"
SELECT
    (SELECT count(*) FROM organisation_profile.schema_migrations),
    (SELECT count(*) FROM organisation_profile.organisation_profiles),
    (SELECT count(*) FROM organisation_profile.organisation_profile_templates),
    (SELECT count(*) FROM organisation_profile.organisation_profile_template_versions),
    (SELECT count(*) FROM organisation_profile.organisation_profile_versions),
    (SELECT count(*) FROM organisation_profile.organisation_profile_version_domains);
"@
}

try {
    if ($settings.Password) {
        $env:PGPASSWORD = $settings.Password
    }

    $sourceCounts = Get-QualificationCounts -Database $settings.Database

    if (Test-Path -LiteralPath $ArtifactPath) {
        Remove-Item -LiteralPath $ArtifactPath -Force
    }

    $dumpArguments = @(
        "-h", $settings.Host,
        "-p", $settings.Port,
        "-U", $settings.User,
        "-d", $settings.Database,
        "--format=custom",
        "--no-owner",
        "--no-privileges",
        "--schema=identity_access",
        "--schema=organization_directory",
        "--schema=organisation_profile",
        "--file", $ArtifactPath
    )

    & pg_dump @dumpArguments
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL backup creation failed."
    }

    if (-not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) {
        throw "PostgreSQL backup artifact was not created."
    }

    if ((Get-Item -LiteralPath $ArtifactPath).Length -le 0) {
        throw "PostgreSQL backup artifact is empty."
    }

    $restoreList = & pg_restore --list $ArtifactPath
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL backup artifact could not be inspected."
    }

    $restoreText = ($restoreList | Out-String)
    foreach ($marker in @(
        "SCHEMA - identity_access",
        "SCHEMA - organization_directory",
        "SCHEMA - organisation_profile",
        "TABLE organisation_profile organisation_profiles",
        "TABLE organisation_profile organisation_profile_template_versions",
        "TABLE organisation_profile organisation_profile_versions"
    )) {
        if (-not $restoreText.Contains($marker)) {
            throw "Backup artifact is missing required restore marker '$marker'."
        }
    }

    $exists = Invoke-QualificationPsql `
        -Database $MaintenanceDatabase `
        -Sql "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_database WHERE datname = '$targetDatabase') THEN '1' ELSE '0' END;"

    if ($exists -ne "0") {
        throw "Disposable restore database '$targetDatabase' already exists."
    }

    Invoke-QualificationPsql `
        -Database $MaintenanceDatabase `
        -Sql "CREATE DATABASE $targetDatabase WITH TEMPLATE template0;" | Out-Null

    $targetCreated = $true

    $restoreArguments = @(
        "-h", $settings.Host,
        "-p", $settings.Port,
        "-U", $settings.User,
        "-d", $targetDatabase,
        "--no-owner",
        "--no-privileges",
        "--exit-on-error",
        $ArtifactPath
    )

    & pg_restore @restoreArguments
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL restore into disposable database failed."
    }

    $restoredCounts = Get-QualificationCounts -Database $targetDatabase

    if ($restoredCounts -ne $sourceCounts) {
        throw "Restored OrganisationProfile row counts differ from source. Source=[$sourceCounts], restored=[$restoredCounts]."
    }

    $env:ORGANISATION_PROFILE_POSTGRES_DATABASE = $targetDatabase

    & (Join-Path $PSScriptRoot "verify-migration-integrity.ps1")
    if (-not $?) {
        throw "Restored migration integrity validation failed."
    }

    & (Join-Path $PSScriptRoot "verify-schema.ps1")
    if (-not $?) {
        throw "Restored schema validation failed."
    }

    & (Join-Path $PSScriptRoot "verify-template-catalog.ps1")
    if (-not $?) {
        throw "Restored template catalog validation failed."
    }

    & (Join-Path $PSScriptRoot "verify-effective-composition.ps1")
    if (-not $?) {
        throw "Restored effective composition validation failed."
    }

    Write-Host "OrganisationProfile backup/restore qualification: GREEN"
}
finally {
    if ($null -eq $previousDatabaseOverride) {
        Remove-Item Env:ORGANISATION_PROFILE_POSTGRES_DATABASE -ErrorAction SilentlyContinue
    } else {
        $env:ORGANISATION_PROFILE_POSTGRES_DATABASE = $previousDatabaseOverride
    }

    if ($targetCreated) {
        try {
            Invoke-QualificationPsql `
                -Database $MaintenanceDatabase `
                -Sql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$targetDatabase' AND pid <> pg_backend_pid();" | Out-Null

            Invoke-QualificationPsql `
                -Database $MaintenanceDatabase `
                -Sql "DROP DATABASE IF EXISTS $targetDatabase;" | Out-Null
        }
        catch {
            Write-Warning "Disposable restore database cleanup failed: $($_.Exception.Message)"
        }
    }

    if (-not $KeepArtifact -and (Test-Path -LiteralPath $ArtifactPath)) {
        Remove-Item -LiteralPath $ArtifactPath -Force -ErrorAction SilentlyContinue
    }

    if ($null -eq $previousPassword) {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:PGPASSWORD = $previousPassword
    }
}
