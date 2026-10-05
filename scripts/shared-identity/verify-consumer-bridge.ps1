[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Consumer Bridge file is missing: $RelativePath"
    }
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    Require-File $RelativePath
    $text = [System.IO.File]::ReadAllText((Join-Path $root $RelativePath))
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required Consumer Bridge marker '$Needle'."
    }
}

foreach ($relativePath in @(
    "packages/auth/src/administration.ts",
    "packages/next/src/server/administration.ts",
    "docs/shared-identity/CONSUMER_BRIDGE.md",
    "docs/shared-identity/validation-manifests/CONSUMER_BRIDGE.md"
)) {
    Require-File $relativePath
}

Require-Text "packages/auth/src/client.ts" "readonly administration: GenericIdentityAdministrationClient"
Require-Text "packages/auth/src/administration.ts" "GenericIdentityAdministrationClient"
Require-Text "packages/auth/src/administration.ts" "GenericIdentityTenantUsersClient"
Require-Text "packages/next/src/server/administration.ts" "session.requireCredential()"
Require-Text "packages/next/src/server/administration.ts" "createNextTenantAdministrationContext"
Require-Text "packages/contracts/src/identity.ts" "IdentityTenantUserRecord"
Require-Text "packages/contracts/src/identity.ts" "IdentityTenantUserListOptions"

$authPackage = [System.IO.File]::ReadAllText((Join-Path $root "packages/auth/package.json")) | ConvertFrom-Json
$nextPackage = [System.IO.File]::ReadAllText((Join-Path $root "packages/next/package.json")) | ConvertFrom-Json
$reactPackage = [System.IO.File]::ReadAllText((Join-Path $root "packages/react/package.json")) | ConvertFrom-Json

foreach ($pair in @(
    @($authPackage, "@generic-identity/auth"),
    @($nextPackage, "@generic-identity/next"),
    @($reactPackage, "@generic-identity/react")
)) {
    $package = $pair[0]
    $name = $pair[1]
    $rootExport = $package.exports."."
    if ($null -eq $rootExport.import -or $null -eq $rootExport.default) {
        throw "$name must expose a runtime source entry for local pre-publication integration."
    }
}

if ($null -eq $authPackage.exports."./administration") {
    throw "@generic-identity/auth must export the administration facade."
}


# Raw-source packages are consumed directly by linked Next.js applications before Release Qualification emits publishable artifacts.
# Relative .js specifiers would point at files that do not exist in src and fail under Turbopack.
$rawSourceRoots = @(
    "packages/contracts/src",
    "packages/auth/src",
    "packages/react/src",
    "packages/next/src"
)
foreach ($rawSourceRoot in $rawSourceRoots) {
    $absoluteRoot = Join-Path $root $rawSourceRoot
    Get-ChildItem $absoluteRoot -Recurse -File | Where-Object { $_.Extension -in @(".ts", ".tsx") } | ForEach-Object {
        $source = [System.IO.File]::ReadAllText($_.FullName)
        if ($source -match '["'']\.\.?/[^"'']+\.js["'']') {
            throw "Linked raw-source package contains a relative .js specifier that Turbopack cannot resolve before emit: $($_.FullName)"
        }
    }
}

Write-Host "Shared Identity Consumer Bridge consumer bridge validation: GREEN"
