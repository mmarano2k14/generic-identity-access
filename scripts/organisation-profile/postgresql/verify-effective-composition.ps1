$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

. (Join-Path $PSScriptRoot "common.ps1")

Invoke-OrganisationProfilePsqlFile `
    -Path (Join-Path $PSScriptRoot "validate-effective-composition.sql")

Write-Host "OrganisationProfile effective composition schema validation: GREEN"
