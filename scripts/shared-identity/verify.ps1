[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

& (Join-Path $PSScriptRoot "verify-professional-nomenclature.ps1")
if (-not $?) {
    throw "Generic Identity professional nomenclature validation failed."
}

& (Join-Path $PSScriptRoot "verify-structure.ps1")
if (-not $?) {
    throw "Shared Identity baseline and structure validation failed."
}

& (Join-Path $PSScriptRoot "verify-public-contracts.ps1")
if (-not $?) {
    throw "Shared Identity public contracts source validation failed."
}

& (Join-Path $PSScriptRoot "verify-authentication-sdk.ps1")
if (-not $?) {
    throw "Shared Identity authentication and authorization SDK validation failed."
}

& (Join-Path $PSScriptRoot "verify-react-foundation.ps1")
if (-not $?) {
    throw "Shared Identity React foundation validation failed."
}

& (Join-Path $PSScriptRoot "verify-shared-pages.ps1")
if (-not $?) {
    throw "Shared Identity shared React pages validation failed."
}

& (Join-Path $PSScriptRoot "verify-theme-and-components.ps1")
if (-not $?) {
    throw "Shared Identity theme and component override validation failed."
}

& (Join-Path $PSScriptRoot "verify-nextjs-integration.ps1")
if (-not $?) {
    throw "Shared Identity Next.js integration validation failed."
}

& (Join-Path $PSScriptRoot "verify-consumer-bridge.ps1")
if (-not $?) {
    throw "Shared Identity consumer bridge validation failed."
}

& (Join-Path $PSScriptRoot "verify-local-package-artifacts.ps1")
if (-not $?) {
    throw "Shared Identity local package artifact validation failed."
}

& (Join-Path $PSScriptRoot "verify-consumer-dev-admin-authority.ps1")
if (-not $?) {
    throw "Shared Identity consumer development administrator authority validation failed."
}


& (Join-Path $PSScriptRoot "verify-release-packaging.ps1")
if (-not $?) {
    throw "Shared Identity release packaging validation failed."
}

& (Join-Path $PSScriptRoot "verify-sdk-inventory.ps1")
if (-not $?) {
    throw "Shared Identity SDK inventory/category validation failed."
}

& (Join-Path $PSScriptRoot "verify-account-directory.ps1")
if (-not $?) {
    throw "Shared Identity Account and Directory validation failed."
}

& (Join-Path $PSScriptRoot "verify-organizations.ps1")
if (-not $?) {
    throw "Shared Identity Organizations validation failed."
}


& (Join-Path $PSScriptRoot "verify-access-control.ps1")
if (-not $?) {
    throw "Shared Identity Access Control validation failed."
}

& (Join-Path $PSScriptRoot "verify-application-security.ps1")
if (-not $?) {
    throw "Generic Identity Application Security source validation failed."
}

& (Join-Path $PSScriptRoot "verify-security-operations.ps1")
if (-not $?) {
    throw "Generic Identity Security Operations source validation failed."
}

& (Join-Path $PSScriptRoot "verify-membership-parity.ps1")
if (-not $?) { throw "Generic Identity Membership SDK validation failed." }

& (Join-Path $PSScriptRoot "verify-entity-reference-autocomplete.ps1")
if (-not $?) { throw "Generic Identity entity-reference autocomplete validation failed." }

& (Join-Path $PSScriptRoot "verify-groups-parity.ps1")
if (-not $?) { throw "Generic Identity Groups parity validation failed." }

& (Join-Path $PSScriptRoot "verify-managed-policies-parity.ps1")
if (-not $?) { throw "Generic Identity Managed Policies parity validation failed." }

& (Join-Path $PSScriptRoot "verify-resource-scopes-parity.ps1")
if (-not $?) { throw "Generic Identity Resource Scopes parity validation failed." }

& (Join-Path $PSScriptRoot "verify-delegated-authority-parity.ps1")
if (-not $?) { throw "Generic Identity Delegated Authority parity validation failed." }

& (Join-Path $PSScriptRoot "verify-mfa-parity.ps1")
if (-not $?) { throw "Generic Identity MFA parity validation failed." }


& (Join-Path $PSScriptRoot "verify-security-audit-parity.ps1")
if (-not $?) { throw "Generic Identity Security Audit parity validation failed." }

& (Join-Path $PSScriptRoot "verify-sessions-parity.ps1")
if (-not $?) { throw "Generic Identity Sessions parity validation failed." }

Write-Host "Shared Identity integration validation: GREEN"
