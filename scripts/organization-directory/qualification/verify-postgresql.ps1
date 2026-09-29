$ErrorActionPreference = "Stop"

$postgresqlRoot = Resolve-Path (Join-Path $PSScriptRoot "..\postgresql")

$checks = @(
    "verify-migration-integrity.ps1",
    "verify-organizations.ps1",
    "verify-memberships.ps1",
    "verify-resource-scope-links.ps1",
    "verify-store.ps1"
)

foreach ($check in $checks) {
    $path = Join-Path $postgresqlRoot $check

    if (-not (Test-Path $path)) {
        throw "Organization Directory PostgreSQL qualification check '$check' was not found."
    }

    Write-Host ""
    Write-Host "Running $check..."
    & $path

    if ($LASTEXITCODE -ne 0) {
        throw "Organization Directory PostgreSQL qualification failed at '$check'."
    }
}

Write-Host ""
Write-Host "Organization Directory PostgreSQL qualification: GREEN"
