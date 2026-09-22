$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH. Install PostgreSQL client tools first."
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

& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName `
    -f (Join-Path $root "scripts\postgresql\validate-concurrency.sql")
if ($LASTEXITCODE -ne 0) { throw "PostgreSQL concurrency validation failed." }

Write-Host "PostgreSQL scope isolation and optimistic-concurrency validation passed."
