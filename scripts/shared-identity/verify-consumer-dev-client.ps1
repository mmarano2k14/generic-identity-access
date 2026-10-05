[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$runner = Join-Path $root "scripts/authentication/run-dev-admin-api.ps1"
$routing = Join-Path $root "config/routing.admin-local.example.json"

$runnerText = [System.IO.File]::ReadAllText($runner)
foreach ($marker in @(
    "Clients__1__ClientId' = 'consumer-web'",
    "Clients__1__ApplicationKey' = 'consumer-app'",
    "Clients__1__AuthenticationContextKey' = 'consumer-primary'",
    "Clients__1__RedirectUris__0' = 'http://localhost:3002/settings/access'"
)) {
    if ($runnerText.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Missing consumer application development authentication client marker: $marker"
    }
}

$config = Get-Content $routing -Raw | ConvertFrom-Json
$route = @($config.routes | Where-Object { $_.applicationKey -eq 'consumer-app' -and $_.identityScopeId -eq '00000000-0000-0000-0000-000000000001' })
if ($route.Count -ne 1) { throw "Expected exactly one consumer application local identity route." }
$context = @($config.authenticationContexts | Where-Object { $_.key -eq 'consumer-primary' -and $_.applicationKey -eq 'consumer-app' })
if ($context.Count -ne 1) { throw "Expected exactly one consumer application local authentication context." }

Write-Host "Consumer application local Generic Identity client configuration: GREEN"
