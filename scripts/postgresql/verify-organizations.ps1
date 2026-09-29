$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "..\organization-directory\postgresql\verify-organizations.ps1")
if (-not $?) { throw "Organization Directory PostgreSQL validation failed." }
