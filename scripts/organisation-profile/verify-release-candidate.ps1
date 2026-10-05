[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $true)]
    [string]$BrowserEvidencePath,

    [switch]$SkipBackupRestore
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

& (Join-Path $PSScriptRoot "verify.ps1") -Configuration $Configuration
if (-not $?) {
    throw "OrganisationProfile automated qualification failed."
}

& (Join-Path $PSScriptRoot "verify-browser-evidence.ps1") `
    -EvidencePath $BrowserEvidencePath
if (-not $?) {
    throw "OrganisationProfile browser evidence qualification failed."
}

if ($SkipBackupRestore) {
    Write-Warning "Backup/restore qualification skipped. Release-candidate qualification is PARTIAL."
    return
}

& (Join-Path $PSScriptRoot "postgresql\verify-backup-restore.ps1")
if (-not $?) {
    throw "OrganisationProfile backup/restore qualification failed."
}

Write-Host "OrganisationProfile Qualification and Hardening release-candidate qualification: GREEN"
