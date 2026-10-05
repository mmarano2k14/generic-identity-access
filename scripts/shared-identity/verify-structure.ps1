[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-Directory([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Container)) {
        throw "Required directory is missing: $RelativePath"
    }
}

function Assert-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }
}

$requiredDirectories = @(
    "src/IdentityAccess.Api",
    "src/IdentityAccess.Application",
    "src/IdentityAccess.Contracts",
    "src/IdentityAccess.Domain",
    "src/IdentityAccess.Authorization",
    "src/IdentityAccess.Infrastructure.Authentication",
    "src/IdentityAccess.Infrastructure.ConfigurationRouting",
    "src/IdentityAccess.Infrastructure.PostgreSql",
    "src/IdentityAccess.Rbac",
    "src/IdentityAccess.Rbac.MultiplexedAdapter",
    "src/IdentityAccess.Mfa.Totp",
    "src/IdentityAccess.Mfa.Recovery",
    "src/IdentityAccess.Mfa.WebAuthn",
    "src/OrganizationDirectory.Application",
    "src/OrganizationDirectory.Contracts",
    "src/OrganizationDirectory.Domain",
    "src/OrganizationDirectory.Infrastructure.PostgreSql",
    "src/OrganisationProfile.Application",
    "src/OrganisationProfile.Domain",
    "src/OrganisationProfile.Infrastructure.PostgreSql",
    "clients/typescript",
    "examples/nextjs/admin",
    "scripts/organization-directory",
    "scripts/organisation-profile",
    "docs/organization-directory",
    "docs/organisation-profile",
    "packages/contracts",
    "packages/auth",
    "packages/react",
    "packages/next"
)

foreach ($relativePath in $requiredDirectories) {
    Assert-Directory $relativePath
}

$requiredFiles = @(
    "IdentityAccess.sln",
    "clients/typescript/package.json",
    "clients/typescript/src/index.ts",
    "examples/nextjs/admin/package.json",
    "examples/nextjs/admin/styles/identity-access-admin.css",
    "packages/README.md",
    "packages/contracts/README.md",
    "packages/auth/README.md",
    "packages/react/README.md",
    "packages/next/README.md",
    "docs/shared-identity/SHARED_IDENTITY_EXTRACTION_BASELINE.md"
)

foreach ($relativePath in $requiredFiles) {
    Assert-File $relativePath
}

$clientPackagePath = Join-Path $root "clients/typescript/package.json"
$clientPackage = Get-Content $clientPackagePath -Raw | ConvertFrom-Json
if ($clientPackage.name -ne "@identity-access/client") {
    throw "Baseline and Structure must not rename the existing TypeScript client package."
}
if ($clientPackage.private -ne $true) {
    throw "Baseline and Structure must not change the existing TypeScript client publication state."
}

$hostPackagePath = Join-Path $root "examples/nextjs/admin/package.json"
$hostPackage = Get-Content $hostPackagePath -Raw | ConvertFrom-Json
$existingClientDependency = $hostPackage.dependencies.'@identity-access/client'
if ($existingClientDependency -ne "file:../../../clients/typescript") {
    throw "Baseline and Structure must not redirect the existing Next.js host away from the proven TypeScript client."
}

# Baseline and Structure reserved four public package boundaries. the Public Contracts, Authentication SDK, React Foundation and Next.js Integration milestones have
# now activated contracts, auth, react and next respectively. Baseline and Structure therefore
# keeps validating the directory/README baseline while each owning milestone validates
# the runtime files added under its boundary.

$solution = [System.IO.File]::ReadAllText((Join-Path $root "IdentityAccess.sln"))
$solutionMarkers = @(
    "IdentityAccess.Api",
    "IdentityAccess.Tests",
    "OrganizationDirectory.Domain",
    "OrganizationDirectory.Tests",
    "OrganisationProfile.Domain",
    "OrganisationProfile.QualificationProbe"
)
foreach ($marker in $solutionMarkers) {
    if ($solution.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Baseline and Structure must preserve the existing solution composition; missing marker '$marker'."
    }
}

Write-Host "Shared Identity Baseline and Structure structure validation: GREEN"
