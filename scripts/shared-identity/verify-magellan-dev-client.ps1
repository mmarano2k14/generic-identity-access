[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$runner = Join-Path $root "scripts/authentication/run-dev-admin-api.ps1"
$routing = Join-Path $root "config/routing.admin-local.example.json"

$runnerText = [System.IO.File]::ReadAllText($runner)
foreach ($marker in @(
    "Clients__1__ClientId' = 'magellan-ux'",
    "Clients__1__ApplicationKey' = 'magellan'",
    "Clients__1__AuthenticationContextKey' = 'magellan-primary'",
    "Clients__1__RedirectUris__0' = 'http://localhost:3002/settings/access'"
)) {
    if ($runnerText.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Missing MAGELLAN development authentication client marker: $marker"
    }
}

$config = Get-Content $routing -Raw | ConvertFrom-Json
$route = @($config.routes | Where-Object { $_.applicationKey -eq 'magellan' -and $_.identityScopeId -eq '00000000-0000-0000-0000-000000000001' })
if ($route.Count -ne 1) { throw "Expected exactly one MAGELLAN local identity route." }
$context = @($config.authenticationContexts | Where-Object { $_.key -eq 'magellan-primary' -and $_.applicationKey -eq 'magellan' })
if ($context.Count -ne 1) { throw "Expected exactly one MAGELLAN local authentication context." }

Write-Host "MAGELLAN local Generic Identity client configuration: GREEN"
