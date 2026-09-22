$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
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

$metadataTableExists = Invoke-PsqlText -Sql @"
SELECT CASE
    WHEN to_regclass('identity_access.schema_migrations') IS NULL THEN '0'
    ELSE '1'
END;
"@

if ($metadataTableExists -ne "1") {
    throw "Migration metadata is not initialized. Run apply-default-schema.ps1 first."
}

$checksumColumnExists = Invoke-PsqlText -Sql @"
SELECT CASE
    WHEN EXISTS
    (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'identity_access'
          AND table_name = 'schema_migrations'
          AND column_name = 'checksum'
    )
    THEN '1'
    ELSE '0'
END;
"@

if ($checksumColumnExists -ne "1") {
    throw "Migration checksum metadata is not initialized. Run apply-default-schema.ps1 first."
}

$knownVersions = New-Object System.Collections.Generic.List[int]

foreach ($migration in $migrations) {
    if ($migration.Name -notmatch '^(\d{4})_.*\.sql$') {
        throw "Invalid migration filename '$($migration.Name)'."
    }

    $version = [int]$Matches[1]
    $knownVersions.Add($version)
    $checksum = Get-MigrationChecksum $migration.FullName

    $metadata = Invoke-PsqlText -Sql @"
SELECT name, COALESCE(checksum, '')
FROM identity_access.schema_migrations
WHERE version = $version;
"@

    if (-not $metadata) {
        throw "Migration $($migration.Name) is not recorded as applied."
    }

    $parts = $metadata -split '\|', 2
    $recordedName = $parts[0]
    $recordedChecksum = if ($parts.Count -gt 1) { $parts[1].Trim() } else { "" }

    if ($recordedName -ne $migration.Name) {
        throw ("Migration integrity validation failed for version {0}: filename mismatch." -f $version)
    }

    if (-not $recordedChecksum) {
        throw "Migration $($migration.Name) does not yet have a recorded checksum. Run apply-default-schema.ps1 first."
    }

    if ($recordedChecksum -ne $checksum) {
        throw "Migration integrity validation failed for $($migration.Name): checksum mismatch."
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

Write-Host "Migration integrity validation passed."
