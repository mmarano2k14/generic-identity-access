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
$root = Split-Path -Parent $PSScriptRoot

$production = Join-Path $PSScriptRoot "verify-production-qualification.ps1"
$browser = Join-Path $PSScriptRoot "verify-admin-ui-browser-qualification.ps1"

Write-Host "Running R4 final production qualification..."
$productionArgs = @{
    RbacReferenceDirectory = $RbacReferenceDirectory
    Configuration = $Configuration
}
if ($SkipBackupRestore) { $productionArgs.SkipBackupRestore = $true }
& $production @productionArgs

if ($SkipBrowserQualification) {
    Write-Warning "Browser qualification was explicitly skipped; R4 final qualification is partial."
}
else {
    Write-Host "Production qualification is complete."
    Write-Host "Start the Identity & Access administration API on http://127.0.0.1:5080 and the Next.js admin host on http://127.0.0.1:3000 in separate terminals."
    [void](Read-Host "When both hosts are ready, press Enter to continue with real-browser qualification")
    Write-Host "Running R4 real-browser administration qualification..."
    & $browser
}

if ($SkipBackupRestore -or $SkipBrowserQualification) {
    Write-Warning "R4 final qualification completed with explicit skips. Do not describe this run as fully GREEN."
}
else {
    Write-Host "R4 final qualification completed successfully."
}
