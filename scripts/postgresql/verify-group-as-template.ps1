param(
    [string]$DatabaseName = "generic_identity_access_default",
    [string]$PostgresUser = "postgres"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
& psql -U $PostgresUser -v ON_ERROR_STOP=1 -d $DatabaseName -f (Join-Path $root "scripts\postgresql\validate-group-as-template.sql")
if ($LASTEXITCODE -ne 0) { throw "Group-as-template PostgreSQL validation failed." }
Write-Host "Group-as-template PostgreSQL validation: GREEN"
