$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH. Install PostgreSQL client tools first."
}

function Invoke-PsqlText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Sql
    )

    $output = & psql `
        -U $postgresUser `
        -v ON_ERROR_STOP=1 `
        -d $databaseName `
        -At `
        -F "|" `
        -c $Sql

    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL command failed."
    }

    if ($null -eq $output) {
        return ""
    }

    return ($output | Out-String).Trim()
}

function Get-MigrationChecksum([string]$Path) {
    $sql = [System.IO.File]::ReadAllText($Path)
    $canonical = $sql.Replace("`r`n", "`n").Replace("`r", "`n")
    $encoding = [System.Text.UTF8Encoding]::new($false)
    $bytes = $encoding.GetBytes($canonical)
    $sha = [System.Security.Cryptography.SHA256]::Create()

    try {
        $hash = $sha.ComputeHash($bytes)
        return -join ($hash | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $sha.Dispose()
    }
}

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
    $env:IDENTITY_ACCESS_POSTGRES_DATABASE
} else {
    "generic_identity_access_default"
}

$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) {
    $env:IDENTITY_ACCESS_POSTGRES_USER
} else {
    "postgres"
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$migrations = Get-ChildItem `
    (Join-Path $root "src\IdentityAccess.Infrastructure.PostgreSql\Migrations") `
    -Filter "*.sql" |
    Sort-Object Name

if (-not $migrations) {
    throw "No PostgreSQL migrations were found."
}

Write-Host "Applying identity schema to '$databaseName' as '$postgresUser'..."

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c @"
CREATE SCHEMA IF NOT EXISTS identity_access;

CREATE TABLE IF NOT EXISTS identity_access.schema_migrations
(
    version integer PRIMARY KEY,
    name varchar(200) NOT NULL,
    checksum char(64) NULL,
    applied_at timestamptz NOT NULL DEFAULT transaction_timestamp()
);

ALTER TABLE identity_access.schema_migrations
    ADD COLUMN IF NOT EXISTS checksum char(64) NULL;
"@

if ($LASTEXITCODE -ne 0) {
    throw "Failed to initialize identity_access migration metadata."
}

$knownVersions = New-Object System.Collections.Generic.List[int]

foreach ($migration in $migrations) {
    if ($migration.Name -notmatch '^(\d{4})_.*\.sql$') {
        throw "Invalid migration filename '$($migration.Name)'. Expected NNNN_name.sql."
    }

    $version = [int]$Matches[1]
    $knownVersions.Add($version)
    $checksum = Get-MigrationChecksum $migration.FullName

    $metadata = Invoke-PsqlText -Sql @"
SELECT name, COALESCE(checksum, '')
FROM identity_access.schema_migrations
WHERE version = $version;
"@

    if ($metadata) {
        $parts = $metadata -split '\|', 2
        $recordedName = $parts[0]
        $recordedChecksum = if ($parts.Count -gt 1) { $parts[1].Trim() } else { "" }

        if ($recordedName -ne $migration.Name) {
            throw ("Migration integrity validation failed for version {0}: filename mismatch." -f $version)
        }

        if (-not $recordedChecksum) {
            & psql `
                -U $postgresUser `
                -v ON_ERROR_STOP=1 `
                -d $databaseName `
                -c "UPDATE identity_access.schema_migrations SET checksum = '$checksum' WHERE version = $version AND checksum IS NULL;"

            if ($LASTEXITCODE -ne 0) {
                throw "Failed to record checksum for migration $($migration.Name)."
            }

            Write-Host "Adopted checksum for $($migration.Name)."
            continue
        }

        if ($recordedChecksum -ne $checksum) {
            throw "Migration integrity validation failed for $($migration.Name): checksum mismatch."
        }

        Write-Host "Skipping $($migration.Name) (already applied and checksum verified)."
        continue
    }

    Write-Host "Applying $($migration.Name)..."
    $escapedName = $migration.Name.Replace("'", "''")

    & psql `
        -U $postgresUser `
        -v ON_ERROR_STOP=1 `
        -d $databaseName `
        --single-transaction `
        -f $migration.FullName `
        -c "INSERT INTO identity_access.schema_migrations(version, name, checksum) VALUES ($version, '$escapedName', '$checksum');"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to apply $($migration.Name)."
    }
}

if ($knownVersions.Count -eq 0) {
    throw "No valid migration versions were discovered."
}

$knownVersionList = ($knownVersions | ForEach-Object { $_.ToString() }) -join ","

$unknownVersions = Invoke-PsqlText -Sql @"
SELECT COALESCE(string_agg(version::text, ',' ORDER BY version), '')
FROM identity_access.schema_migrations
WHERE version NOT IN ($knownVersionList);
"@

if ($unknownVersions) {
    throw "Migration integrity validation failed: unknown applied versions [$unknownVersions]."
}

& psql `
    -U $postgresUser `
    -v ON_ERROR_STOP=1 `
    -d $databaseName `
    -c "ALTER TABLE identity_access.schema_migrations ALTER COLUMN checksum SET NOT NULL;"

if ($LASTEXITCODE -ne 0) {
    throw "Failed to finalize migration checksum metadata."
}

Write-Host "Schema applied and migration integrity verified."
