[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

& (Join-Path $PSScriptRoot "verify-pack-01-structure.ps1")
if (-not $?) {
    throw "Shared Identity Pack 1 structure validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-02-contracts.ps1")
if (-not $?) {
    throw "Shared Identity Pack 2 public contracts source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-03-auth.ps1")
if (-not $?) {
    throw "Shared Identity Pack 3 auth SDK source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-04-react.ps1")
if (-not $?) {
    throw "Shared Identity Pack 4 React foundation source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-05-pages.ps1")
if (-not $?) {
    throw "Shared Identity Pack 5 shared pages source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-06-theme.ps1")
if (-not $?) {
    throw "Shared Identity Pack 6 theme/component override source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-07-next.ps1")
if (-not $?) {
    throw "Shared Identity Pack 7 Next.js integration source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-07-closure.ps1")
if (-not $?) {
    throw "Shared Identity Pack 7 closure consumer bridge validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-09-local-package-artifacts.ps1")
if (-not $?) {
    throw "Shared Identity Pack 9 local package artifact source validation failed."
}

& (Join-Path $PSScriptRoot "verify-pack-09-magellan-dev-admin-authority.ps1")
if (-not $?) {
    throw "Shared Identity Pack 9 MAGELLAN development administrator authority source validation failed."
}


& (Join-Path $PSScriptRoot "verify-pack-10-release.ps1")
if (-not $?) {
    throw "Shared Identity Pack 10 release packaging validation failed."
}

Write-Host "Shared Identity integration validation: GREEN"
