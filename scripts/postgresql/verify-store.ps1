$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "..\organization-directory\postgresql\verify-store.ps1")
if (-not $?) { throw "Organization Directory store verification failed." }
