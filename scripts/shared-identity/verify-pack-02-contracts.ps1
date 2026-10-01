[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Pack 2 file is missing: $RelativePath"
    }
}

function Read-Source([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    Assert-File $RelativePath
    return [System.IO.File]::ReadAllText($path)
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required Pack 2 marker '$Needle'."
    }
}

function Reject-Text([string]$RelativePath, [string]$Needle, [string]$Reason) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -ge 0) {
        throw "$Reason Found '$Needle' in '$RelativePath'."
    }
}

$requiredFiles = @(
    "packages/contracts/package.json",
    "packages/contracts/tsconfig.json",
    "packages/contracts/src/index.ts",
    "packages/contracts/src/administration.ts",
    "packages/contracts/src/authorization.ts",
    "packages/contracts/src/errors.ts",
    "packages/contracts/src/identity.ts",
    "packages/contracts/src/mfa.ts",
    "packages/contracts/src/policies.ts",
    "packages/contracts/src/security-manifest.ts",
    "packages/contracts/src/session.ts",
    "packages/contracts/test/consumer-contracts.ts",
    "packages/contracts/README.md",
    "docs/shared-identity/PACK_02_PUBLIC_CONTRACTS.md",
    "docs/shared-identity/packs/PACK_02_PUBLIC_CONTRACTS_MANIFEST.md"
)
foreach ($relativePath in $requiredFiles) {
    Assert-File $relativePath
}

$packagePath = Join-Path $root "packages/contracts/package.json"
$package = Get-Content $packagePath -Raw | ConvertFrom-Json
if ($package.name -ne "@generic-identity/contracts") {
    throw "Pack 2 contracts package name must remain '@generic-identity/contracts'."
}
try {
    $packageVersion = [System.Version]$package.version
}
catch {
    throw "Pack 2 contracts package version must be a valid numeric semantic version."
}
if ($packageVersion -lt [System.Version]"0.1.0") {
    throw "Pack 2 contracts package version must not regress below 0.1.0."
}
if ($package.private -ne $true) {
    throw "Pack 2 contracts package must remain private until release qualification."
}
if ($package.type -ne "module") {
    throw "Pack 2 contracts package must remain an ESM package."
}
if ($null -ne $package.PSObject.Properties["dependencies"]) {
    throw "The passive contracts package must not acquire runtime dependencies."
}
if ($null -ne $package.PSObject.Properties["peerDependencies"]) {
    throw "The passive contracts package must not acquire framework peer dependencies."
}

$expectedExports = @{
    "." = "./src/index.ts"
    "./administration" = "./src/administration.ts"
    "./authorization" = "./src/authorization.ts"
    "./errors" = "./src/errors.ts"
    "./identity" = "./src/identity.ts"
    "./mfa" = "./src/mfa.ts"
    "./policies" = "./src/policies.ts"
    "./security-manifest" = "./src/security-manifest.ts"
    "./session" = "./src/session.ts"
}
foreach ($exportKey in $expectedExports.Keys) {
    $exportProperty = $package.exports.PSObject.Properties[$exportKey]
    if ($null -eq $exportProperty -or $exportProperty.Value.types -ne $expectedExports[$exportKey]) {
        throw "Pack 2 contracts export '$exportKey' must resolve to '$($expectedExports[$exportKey])'."
    }
}

$tsconfigPath = Join-Path $root "packages/contracts/tsconfig.json"
$tsconfig = Get-Content $tsconfigPath -Raw | ConvertFrom-Json
if ($tsconfig.compilerOptions.strict -ne $true) {
    throw "Pack 2 contracts typecheck must use strict TypeScript mode."
}
if ($tsconfig.compilerOptions.exactOptionalPropertyTypes -ne $true) {
    throw "Pack 2 contracts typecheck must preserve exact optional-property semantics."
}
if ($tsconfig.compilerOptions.noEmit -ne $true) {
    throw "Pack 2 is a type-only bridge and must not emit runtime JavaScript."
}

if ($tsconfig.compilerOptions.module -ne "ESNext" -or $tsconfig.compilerOptions.moduleResolution -ne "Bundler") {
    throw "Pack 2 raw-source contracts must use ESNext + Bundler resolution for linked consumer compilation."
}

$indexPath = "packages/contracts/src/index.ts"
$contractSourceFiles = @(
    "packages/contracts/src/administration.ts",
    "packages/contracts/src/authorization.ts",
    "packages/contracts/src/errors.ts",
    "packages/contracts/src/identity.ts",
    "packages/contracts/src/mfa.ts",
    "packages/contracts/src/policies.ts",
    "packages/contracts/src/security-manifest.ts",
    "packages/contracts/src/session.ts"
)

$contractTextBuilder = New-Object System.Text.StringBuilder
foreach ($relativePath in $contractSourceFiles) {
    [void]$contractTextBuilder.AppendLine((Read-Source $relativePath))
}
$contractText = $contractTextBuilder.ToString()

$requiredContractMarkers = @(
    "IdentityUserRecord",
    "IdentityTenantRecord",
    "IdentityTenantMembershipRecord",
    "IdentityGroupRecord",
    "IdentityManagedPolicyRecord",
    "IdentityManagedGroupPolicyBindingRecord",
    "IdentityCapabilityRequirement",
    "IdentityApplicationSecurityManifestRequest",
    "IdentityAuthorizationBoundary",
    "IdentitySessionValidationResult",
    "IdentityMfaProviderRecord",
    "AuthorizationEvaluationResponse",
    "IdentityAccessErrorCode"
)
foreach ($marker in $requiredContractMarkers) {
    if ($contractText.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "The Pack 2 public contract surface is missing required marker '$marker'."
    }
}

foreach ($relativePath in $contractSourceFiles) {
    Require-Text $relativePath 'from "@identity-access/client"'
    Reject-Text $relativePath '../../../clients/typescript/src/' 'Pack 2 contracts must not escape their package boundary through source-relative imports.'
}

$clientPathMapping = $tsconfig.compilerOptions.paths.PSObject.Properties['@identity-access/client']
if ($null -eq $clientPathMapping -or $clientPathMapping.Value[0] -ne '../../clients/typescript/src/index.ts') {
    throw "Pack 2 contracts must map @identity-access/client to the proven local client source for repository typechecking."
}

# The contracts boundary is passive. Secret-bearing authentication material stays
# behind the authentication SDK/client boundary and must never leak here.
$forbiddenSecretContracts = @(
    "IdentityAccessCredential",
    "IdentityBearerCredential",
    "IdentitySessionCredential",
    "IdentityLocalSession",
    "IdentityOidcTokenSet",
    "IdentityPasswordLoginRequest",
    "IdentitySelfServicePasswordChangeRequest",
    "IdentityRecoveryPasswordResetRequest",
    "IdentityCreatePasswordCredentialRequest",
    "IdentityChangePasswordCredentialRequest"
)
foreach ($marker in $forbiddenSecretContracts) {
    if ($contractText.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Secret-bearing authentication contract '$marker' does not belong in @generic-identity/contracts."
    }
}

# Generic Organization Directory and OrganisationProfile keep their own ownership.
$forbiddenCrossBoundaryContracts = @(
    "IdentityOrganizationRecord",
    "IdentityOrganizationTreeNodeRecord",
    "IdentityOrganizationMembershipRecord",
    "IdentityOrganizationResourceScopeLinkRecord",
    "IdentityOrganisationProfile"
)
foreach ($marker in $forbiddenCrossBoundaryContracts) {
    if ($contractText.IndexOf($marker, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Pack 2 must not absorb cross-boundary contract '$marker'."
    }
}

# Type-only means no runtime implementation can be introduced into this package.
$runtimeMarkers = @(
    "export class ",
    "export function ",
    "export const ",
    "fetch(",
    "localStorage",
    "sessionStorage"
)
foreach ($relativePath in $contractSourceFiles + @($indexPath)) {
    foreach ($marker in $runtimeMarkers) {
        Reject-Text $relativePath $marker "Pack 2 contracts must remain passive and type-only."
    }
}

Require-Text $indexPath 'export type * from "./identity"'
Require-Text $indexPath 'export type * from "./authorization"'
Require-Text $indexPath 'export type * from "./policies"'
Require-Text $indexPath 'export type * from "./security-manifest"'
Require-Text $indexPath 'export type * from "./session"'
Require-Text $indexPath 'export type * from "./mfa"'

$probePath = "packages/contracts/test/consumer-contracts.ts"
Require-Text $probePath 'from "@generic-identity/contracts"'
Reject-Text $probePath "clients/typescript" "The consumer probe must compile only against the new public package name."
Reject-Text $probePath "src/IdentityAccess" "The consumer probe must not reach into backend implementation details."

# Existing proven consumers remain on the legacy package during this additive pack.
$legacyPackage = Get-Content (Join-Path $root "clients/typescript/package.json") -Raw | ConvertFrom-Json
if ($legacyPackage.name -ne "@identity-access/client" -or $legacyPackage.private -ne $true) {
    throw "Pack 2 must not rename or republish the existing TypeScript client."
}
$hostPackage = Get-Content (Join-Path $root "examples/nextjs/admin/package.json") -Raw | ConvertFrom-Json
if ($hostPackage.dependencies.'@identity-access/client' -ne "file:../../../clients/typescript") {
    throw "Pack 2 must not redirect the proven Next.js host before the consumer-integration pack."
}

Write-Host "Shared Identity Pack 2 public contracts source validation: GREEN"
