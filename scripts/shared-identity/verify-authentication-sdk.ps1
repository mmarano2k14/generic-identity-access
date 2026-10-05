[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Authentication and Authorization SDK file is missing: $RelativePath"
    }
}

function Read-Source([string]$RelativePath) {
    Assert-File $RelativePath
    return [System.IO.File]::ReadAllText((Join-Path $root $RelativePath))
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required Authentication and Authorization SDK marker '$Needle'."
    }
}

function Reject-Text([string]$RelativePath, [string]$Needle, [string]$Reason) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Reason Found '$Needle' in '$RelativePath'."
    }
}

$requiredFiles = @(
    "packages/auth/package.json",
    "packages/auth/tsconfig.json",
    "packages/auth/src/authentication.ts",
    "packages/auth/src/authorization.ts",
    "packages/auth/src/authorization-context.ts",
    "packages/auth/src/client.ts",
    "packages/auth/src/errors.ts",
    "packages/auth/src/index.ts",
    "packages/auth/test/consumer-auth.ts",
    "packages/auth/README.md",
    "docs/shared-identity/AUTHORIZATION_SDK.md",
    "docs/shared-identity/validation-manifests/AUTHORIZATION_SDK.md"
)
foreach ($relativePath in $requiredFiles) {
    Assert-File $relativePath
}

$packagePath = Join-Path $root "packages/auth/package.json"
$package = Get-Content $packagePath -Raw | ConvertFrom-Json
if ($package.name -ne "@generic-identity/auth") {
    throw "Auth package name must remain '@generic-identity/auth'."
}
try {
    $packageVersion = [System.Version]$package.version
}
catch {
    throw "Auth package version must be a valid numeric semantic version."
}
if ($packageVersion -lt [System.Version]"0.1.0") {
    throw "Auth package version must not regress below 0.1.0."
}
if ($package.private -ne $true) {
    throw "Auth package must remain private until release qualification."
}
if ($package.type -ne "module") {
    throw "Auth package must remain ESM."
}

$contractsDependency = $package.dependencies.PSObject.Properties['@generic-identity/contracts']
if ($null -eq $contractsDependency -or $contractsDependency.Value -ne "file:../contracts") {
    throw "Auth SDK must depend on the local @generic-identity/contracts boundary."
}
$legacyDependency = $package.dependencies.PSObject.Properties['@identity-access/client']
if ($null -eq $legacyDependency -or $legacyDependency.Value -ne "file:../../clients/typescript") {
    throw "Authentication and Authorization SDK must preserve the explicit temporary legacy runtime bridge."
}
if ($null -ne $package.dependencies.PSObject.Properties['react']) {
    throw "The framework-neutral auth package must not depend on React."
}
if ($null -ne $package.dependencies.PSObject.Properties['next']) {
    throw "The framework-neutral auth package must not depend on Next.js."
}
if ($null -ne $package.PSObject.Properties['peerDependencies']) {
    throw "Auth SDK does not require framework peer dependencies."
}

$tsconfig = Get-Content (Join-Path $root "packages/auth/tsconfig.json") -Raw | ConvertFrom-Json
if ($tsconfig.compilerOptions.strict -ne $true) {
    throw "Auth SDK typecheck must use strict TypeScript mode."
}
if ($tsconfig.compilerOptions.exactOptionalPropertyTypes -ne $true) {
    throw "Auth SDK must preserve exact optional-property semantics."
}
if ($tsconfig.compilerOptions.noEmit -ne $true) {
    throw "Authentication and Authorization SDK remains an extraction bridge and must not publish emitted artifacts yet."
}

if ($tsconfig.compilerOptions.module -ne "ESNext" -or $tsconfig.compilerOptions.moduleResolution -ne "Bundler") {
    throw "Authentication and Authorization SDK raw-source auth SDK must use ESNext + Bundler resolution for linked consumer compilation."
}

$sourceFiles = @(
    "packages/auth/src/authentication.ts",
    "packages/auth/src/authorization.ts",
    "packages/auth/src/authorization-context.ts",
    "packages/auth/src/client.ts",
    "packages/auth/src/errors.ts",
    "packages/auth/src/index.ts"
)

Require-Text "packages/auth/src/client.ts" 'new LegacyIdentityAccessClient(options)'
Require-Text "packages/auth/src/client.ts" 'readonly authentication: GenericIdentityAuthenticationClient'
Require-Text "packages/auth/src/client.ts" 'readonly authorization: GenericIdentityAuthorizationClient'
Require-Text "packages/auth/src/authentication.ts" 'client.authentication.passwordLogin(request, signal)'
Require-Text "packages/auth/src/authentication.ts" 'client.authentication.logout(credential, postLogoutRedirectUri, signal)'
Require-Text "packages/auth/src/authentication.ts" 'client.authentication.validateSession(credential, signal)'
Require-Text "packages/auth/src/authorization.ts" 'client.authorization.evaluate(boundary, requirement, credential, signal)'
Require-Text "packages/auth/src/authorization-context.ts" 'LegacyIdentityAuthorizationContext'
Require-Text "packages/auth/src/authorization-context.ts" 'export { RequireCapability }'
Require-Text "packages/auth/src/errors.ts" 'GenericIdentityClientError'

# The public SDK is a facade. It must not acquire its own transport, storage or
# permission-evaluation implementation while the legacy client is authoritative.
$forbiddenImplementationMarkers = @(
    "fetch(",
    "localStorage",
    "sessionStorage",
    "postgres",
    "redis",
    "parseTrn",
    "parseTRN",
    "trn:",
    "allowed: true",
    "return true;"
)
foreach ($relativePath in $sourceFiles) {
    foreach ($marker in $forbiddenImplementationMarkers) {
        Reject-Text $relativePath $marker "Authentication and Authorization SDK must delegate proven authentication/authorization behavior rather than reimplement it."
    }
    Reject-Text $relativePath 'from "react"' "Auth SDK must remain framework-neutral."
    Reject-Text $relativePath 'from "next' "Auth SDK must not depend on Next.js."
    Reject-Text $relativePath 'src/IdentityAccess.' "Authentication and Authorization SDK TypeScript packages must not import backend CLR implementation paths."
    Reject-Text $relativePath 'OrganizationDirectory' "Authentication and Authorization SDK must not absorb Generic Organization Directory ownership."
    Reject-Text $relativePath 'OrganisationProfile' "Authentication and Authorization SDK must not absorb OrganisationProfile ownership."
}

$indexPath = "packages/auth/src/index.ts"
Require-Text $indexPath 'export * from "./authentication"'
Require-Text $indexPath 'export * from "./authorization"'
Require-Text $indexPath 'export * from "./authorization-context"'
Require-Text $indexPath 'export * from "./client"'
Require-Text $indexPath 'export * from "./errors"'

$probePath = "packages/auth/test/consumer-auth.ts"
Require-Text $probePath 'from "@generic-identity/auth"'
Require-Text $probePath 'from "@generic-identity/contracts"'
Reject-Text $probePath 'clients/typescript' "The Authentication and Authorization SDK consumer probe must compile only against public package names."
Reject-Text $probePath 'src/IdentityAccess' "The Authentication and Authorization SDK consumer probe must not reach into backend implementation details."

# The temporary bridge only expands the legacy root export surface with two
# already-existing request declarations. Existing package identity is unchanged.
$legacyPackage = Get-Content (Join-Path $root "clients/typescript/package.json") -Raw | ConvertFrom-Json
if ($legacyPackage.name -ne "@identity-access/client" -or $legacyPackage.private -ne $true) {
    throw "Authentication and Authorization SDK must not rename or republish the existing TypeScript client."
}
Require-Text "clients/typescript/src/index.ts" "IdentityRecoveryPasswordResetRequest"
Require-Text "clients/typescript/src/index.ts" "IdentitySelfServicePasswordChangeRequest"

# Proven host stays on the legacy package until the dedicated consumer-integration milestone.
$hostPackage = Get-Content (Join-Path $root "examples/nextjs/admin/package.json") -Raw | ConvertFrom-Json
if ($hostPackage.dependencies.'@identity-access/client' -ne "file:../../../clients/typescript") {
    throw "Authentication and Authorization SDK must not redirect the proven Next.js host before the consumer-integration milestone."
}
if ($null -ne $hostPackage.dependencies.PSObject.Properties['@generic-identity/auth']) {
    throw "Authentication and Authorization SDK must not integrate the existing Next.js host prematurely."
}

Write-Host "Shared Identity authentication and authorization SDK source validation: GREEN"
