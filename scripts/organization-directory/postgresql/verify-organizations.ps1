$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql was not found on PATH." }
$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) { $env:IDENTITY_ACCESS_POSTGRES_DATABASE } else { "generic_identity_access_default" }
$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) { $env:IDENTITY_ACCESS_POSTGRES_USER } else { "postgres" }
$sql = Join-Path $PSScriptRoot "validate-organizations.sql"
& psql -U $postgresUser -d $databaseName -v ON_ERROR_STOP=1 -f $sql
if ($LASTEXITCODE -ne 0) { throw "Organization Directory PostgreSQL validation failed." }
Write-Host "Organization Directory PostgreSQL validation: GREEN"
