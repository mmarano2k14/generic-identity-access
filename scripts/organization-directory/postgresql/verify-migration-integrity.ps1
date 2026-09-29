$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql was not found on PATH." }
function Invoke-PsqlText { param([Parameter(Mandatory = $true)][string]$Sql); $output = & psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -At -F "|" -c $Sql; if ($LASTEXITCODE -ne 0) { throw "PostgreSQL command failed." }; if ($null -eq $output) { return "" }; return ($output | Out-String).Trim() }
function Get-MigrationChecksum([string]$Path) { $sql=[System.IO.File]::ReadAllText($Path); $canonical=$sql.Replace("`r`n","`n").Replace("`r","`n"); $bytes=[System.Text.UTF8Encoding]::new($false).GetBytes($canonical); $sha=[System.Security.Cryptography.SHA256]::Create(); try { return -join ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) } finally { $sha.Dispose() } }
$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) { $env:IDENTITY_ACCESS_POSTGRES_DATABASE } else { "generic_identity_access_default" }
$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) { $env:IDENTITY_ACCESS_POSTGRES_USER } else { "postgres" }
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$migrations = Get-ChildItem (Join-Path $root "src\OrganizationDirectory.Infrastructure.PostgreSql\Migrations") -Filter "*.sql" | Sort-Object Name
if (-not $migrations) { throw "No Organization Directory migrations were found." }
$tableExists = Invoke-PsqlText -Sql "SELECT CASE WHEN to_regclass('organization_directory.schema_migrations') IS NULL THEN '0' ELSE '1' END;"
if ($tableExists -ne "1") { throw "Organization Directory migration metadata is not initialized." }
$knownVersions = New-Object System.Collections.Generic.List[int]
foreach ($migration in $migrations) { if ($migration.Name -notmatch '^(\d{4})_.*\.sql$') { throw "Invalid migration filename '$($migration.Name)'." }; $version=[int]$Matches[1]; $knownVersions.Add($version); $checksum=Get-MigrationChecksum $migration.FullName; $metadata=Invoke-PsqlText -Sql "SELECT name, checksum FROM organization_directory.schema_migrations WHERE version = $version;"; if (-not $metadata) { throw "Migration $($migration.Name) is not recorded as applied." }; $parts=$metadata -split '\|',2; if ($parts[0] -ne $migration.Name) { throw "Migration integrity failed for version $version`: filename mismatch." }; if ($parts.Count -lt 2 -or $parts[1].Trim() -ne $checksum) { throw "Migration integrity failed for $($migration.Name): checksum mismatch." } }
$knownVersionList = ($knownVersions | ForEach-Object { $_.ToString() }) -join ","
$unknown = Invoke-PsqlText -Sql "SELECT COALESCE(string_agg(version::text, ',' ORDER BY version), '') FROM organization_directory.schema_migrations WHERE version NOT IN ($knownVersionList);"
if ($unknown) { throw "Organization Directory migration integrity failed: unknown applied versions [$unknown]." }
Write-Host "Organization Directory migration integrity validation: GREEN"
