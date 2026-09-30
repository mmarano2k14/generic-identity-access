$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

. (Join-Path $PSScriptRoot "common.ps1")

$sql =
    Join-Path $PSScriptRoot "validate-schema.sql"

Invoke-OrganisationProfilePsqlFile -Path $sql

Write-Host "OrganisationProfile PostgreSQL schema validation: GREEN"
