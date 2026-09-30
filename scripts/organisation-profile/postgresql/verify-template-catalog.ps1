$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

. (Join-Path $PSScriptRoot "common.ps1")

Invoke-OrganisationProfilePsqlFile `
    -Path (Join-Path $PSScriptRoot "validate-template-catalog.sql")

Write-Host "OrganisationProfile template catalog schema validation: GREEN"
