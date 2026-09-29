$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql was not found on PATH." }

function Invoke-PsqlText {
    param([Parameter(Mandatory = $true)][string]$Sql)
    $output = & psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -At -F "|" -c $Sql
    if ($LASTEXITCODE -ne 0) { throw "PostgreSQL command failed." }
    if ($null -eq $output) { return "" }
    return ($output | Out-String).Trim()
}

function Get-MigrationChecksum([string]$Path) {
    $sql = [System.IO.File]::ReadAllText($Path)
    $canonical = $sql.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($canonical)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return -join ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) }
    finally { $sha.Dispose() }
}

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) { $env:IDENTITY_ACCESS_POSTGRES_DATABASE } else { "generic_identity_access_default" }
$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) { $env:IDENTITY_ACCESS_POSTGRES_USER } else { "postgres" }
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$migrations = Get-ChildItem (Join-Path $root "src\OrganizationDirectory.Infrastructure.PostgreSql\Migrations") -Filter "*.sql" | Sort-Object Name
if (-not $migrations) { throw "No Organization Directory PostgreSQL migrations were found." }

$tenantTable = Invoke-PsqlText -Sql "SELECT CASE WHEN to_regclass('identity_access.tenants') IS NULL THEN '0' ELSE '1' END;"
if ($tenantTable -ne "1") { throw "Identity Access schema is not initialized. Run scripts\postgresql\apply-default-schema.ps1 first." }

Write-Host "Applying organization_directory schema to shared database '$databaseName' as '$postgresUser'..."
& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -c @"
CREATE SCHEMA IF NOT EXISTS organization_directory;
CREATE TABLE IF NOT EXISTS organization_directory.schema_migrations
(
    version integer PRIMARY KEY,
    name varchar(200) NOT NULL,
    checksum char(64) NOT NULL,
    applied_at timestamptz NOT NULL DEFAULT transaction_timestamp()
);
"@
if ($LASTEXITCODE -ne 0) { throw "Failed to initialize organization_directory migration metadata." }

$knownVersions = New-Object System.Collections.Generic.List[int]
foreach ($migration in $migrations) {
    if ($migration.Name -notmatch '^(\d{4})_.*\.sql$') { throw "Invalid Organization Directory migration filename '$($migration.Name)'." }
    $version = [int]$Matches[1]
    $knownVersions.Add($version)
    $checksum = Get-MigrationChecksum $migration.FullName
    $metadata = Invoke-PsqlText -Sql "SELECT name, checksum FROM organization_directory.schema_migrations WHERE version = $version;"
    if ($metadata) {
        $parts = $metadata -split '\|', 2
        if ($parts[0] -ne $migration.Name) { throw "Organization Directory migration integrity failed for version $version`: filename mismatch." }
        if ($parts.Count -lt 2 -or $parts[1].Trim() -ne $checksum) { throw "Organization Directory migration integrity failed for $($migration.Name): checksum mismatch." }
        Write-Host "Skipping $($migration.Name) (already applied and checksum verified)."
        continue
    }
    $escapedName = $migration.Name.Replace("'", "''")
    Write-Host "Applying $($migration.Name)..."
    & psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName --single-transaction -f $migration.FullName -c "INSERT INTO organization_directory.schema_migrations(version, name, checksum) VALUES ($version, '$escapedName', '$checksum');"
    if ($LASTEXITCODE -ne 0) { throw "Failed to apply $($migration.Name)." }
}

$knownVersionList = ($knownVersions | ForEach-Object { $_.ToString() }) -join ","
$unknown = Invoke-PsqlText -Sql "SELECT COALESCE(string_agg(version::text, ',' ORDER BY version), '') FROM organization_directory.schema_migrations WHERE version NOT IN ($knownVersionList);"
if ($unknown) { throw "Organization Directory migration integrity failed: unknown applied versions [$unknown]." }
Write-Host "Organization Directory schema applied to shared Identity Access database: GREEN"
