[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RbacReferenceDirectory,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipBackupRestore,
    [switch]$SkipBrowserQualification
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$production = Join-Path $PSScriptRoot "verify-production-qualification.ps1"
$browser = Join-Path $PSScriptRoot "verify-admin-ui-browser-qualification.ps1"

Write-Host "Running production qualification..."
$productionArgs = @{
    RbacReferenceDirectory = $RbacReferenceDirectory
    Configuration = $Configuration
}
if ($SkipBackupRestore) { $productionArgs.SkipBackupRestore = $true }
& $production @productionArgs

if ($SkipBrowserQualification) {
    Write-Warning "Browser qualification was explicitly skipped; administration qualification is partial."
}
else {
    Write-Host "Production qualification is complete."
    Write-Host "Start the Identity & Access API on http://127.0.0.1:5080 and the Next.js administration host on http://127.0.0.1:3000 in separate terminals."
    [void](Read-Host "When both hosts are ready, press Enter to continue with browser qualification")
    Write-Host "Running real-browser administration qualification..."
    & $browser
}

if ($SkipBackupRestore -or $SkipBrowserQualification) {
    Write-Warning "Administration qualification completed with explicit skips. Do not describe this run as complete."
}
else {
    Write-Host "Administration qualification completed successfully."
}
