[CmdletBinding()]
param(
    [string]$DatabaseName = $(
        if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
            $env:IDENTITY_ACCESS_POSTGRES_DATABASE
        } else {
            "generic_identity_access_default"
        }
    ),
    [string]$PostgresUser = $(
        if ($env:IDENTITY_ACCESS_POSTGRES_USER) {
            $env:IDENTITY_ACCESS_POSTGRES_USER
        } else {
            "postgres"
        }
    ),
    [switch]$KeepRestoredDatabase
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

foreach ($command in @("psql", "pg_dump", "pg_restore", "createdb", "dropdb")) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "$command was not found on PATH. PostgreSQL client tools are required."
    }
}

if ([string]::IsNullOrWhiteSpace($DatabaseName)) {
    throw "A source database name is required."
}

if ([string]::IsNullOrWhiteSpace($PostgresUser)) {
    throw "A PostgreSQL user is required."
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$artifactDirectory = Join-Path $root "artifacts\production-qualification"
New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null

$restoreDatabase = "generic_identity_access_restore_$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
$dumpFile = Join-Path $artifactDirectory "$restoreDatabase.dump"
$previousDatabase = $env:IDENTITY_ACCESS_POSTGRES_DATABASE
$createdRestoreDatabase = $false
$qualificationSucceeded = $false

try {
    Write-Host "Validating source migration integrity before backup..."
    $env:IDENTITY_ACCESS_POSTGRES_DATABASE = $DatabaseName
    & (Join-Path $PSScriptRoot "verify-migration-integrity.ps1")

    Write-Host "Creating a custom-format backup from qualification database '$DatabaseName'..."
    & pg_dump `
        -U $PostgresUser `
        --format=custom `
        --no-owner `
        --no-privileges `
        --file=$dumpFile `
        $DatabaseName

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed with exit code $LASTEXITCODE."
    }

    Write-Host "Creating isolated restore database '$restoreDatabase'..."
    & createdb -U $PostgresUser $restoreDatabase
    if ($LASTEXITCODE -ne 0) {
        throw "createdb failed with exit code $LASTEXITCODE."
    }

    $createdRestoreDatabase = $true

    Write-Host "Restoring backup into isolated database '$restoreDatabase'..."
    & pg_restore `
        -U $PostgresUser `
        --exit-on-error `
        --no-owner `
        --no-privileges `
        --dbname=$restoreDatabase `
        $dumpFile

    if ($LASTEXITCODE -ne 0) {
        throw "pg_restore failed with exit code $LASTEXITCODE."
    }

    $env:IDENTITY_ACCESS_POSTGRES_DATABASE = $restoreDatabase

    Write-Host "Validating restored migration metadata against repository migrations..."
    & (Join-Path $PSScriptRoot "verify-migration-integrity.ps1")

    Write-Host "Validating restored schema structure and secret-safety invariants..."
    & psql `
        -U $PostgresUser `
        -v ON_ERROR_STOP=1 `
        -d $restoreDatabase `
        -f (Join-Path $PSScriptRoot "validate-restored-database.sql")

    if ($LASTEXITCODE -ne 0) {
        throw "Restored database structural validation failed with exit code $LASTEXITCODE."
    }

    $qualificationSucceeded = $true
    Write-Host "Backup and restore qualification passed."
}
finally {
    if ($null -eq $previousDatabase) {
        Remove-Item Env:IDENTITY_ACCESS_POSTGRES_DATABASE -ErrorAction SilentlyContinue
    } else {
        $env:IDENTITY_ACCESS_POSTGRES_DATABASE = $previousDatabase
    }

    $cleanupFailed = $false

    if ($createdRestoreDatabase -and -not $KeepRestoredDatabase) {
        & dropdb -U $PostgresUser --if-exists $restoreDatabase
        if ($LASTEXITCODE -ne 0) {
            $cleanupFailed = $true
            Write-Warning "Restore database cleanup failed for '$restoreDatabase'."
        }
    } elseif ($createdRestoreDatabase) {
        Write-Host "Restored database retained for inspection: $restoreDatabase"
    }

    Remove-Item $dumpFile -Force -ErrorAction SilentlyContinue

    if ($cleanupFailed -and $qualificationSucceeded) {
        throw "Backup/restore validation passed but scratch database cleanup failed."
    }
}
