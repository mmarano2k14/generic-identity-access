param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipPostgreSql
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Invoke-CheckedPowerShellScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [hashtable]$Parameters = @{}
    )

    if (-not (Test-Path $Path)) {
        throw "Required qualification script '$Path' was not found."
    }

    & $Path @Parameters

    if (-not $?) {
        throw "Qualification script '$Path' failed."
    }
}

Write-Host "Running repository verification..."
Invoke-CheckedPowerShellScript `
    -Path (Join-Path $root "scripts\verify.ps1") `
    -Parameters @{ Configuration = $Configuration }

Write-Host ""
Write-Host "Running Organization Directory module verification..."
Invoke-CheckedPowerShellScript `
    -Path (Join-Path $root "scripts\organization-directory\verify.ps1") `
    -Parameters @{ Configuration = $Configuration }

Write-Host ""
Write-Host "Running Organization Directory separation verification..."
Invoke-CheckedPowerShellScript `
    -Path (Join-Path $root "scripts\organization-directory\qualification\verify-separation.ps1")

if ($SkipPostgreSql) {
    Write-Warning "PostgreSQL qualification was skipped. This run is PARTIAL."
    return
}

Write-Host ""
Write-Host "Running Organization Directory PostgreSQL qualification..."
Invoke-CheckedPowerShellScript `
    -Path (Join-Path $root "scripts\organization-directory\qualification\verify-postgresql.ps1")

Write-Host ""
Write-Host "Organization Directory qualification: GREEN"
