[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required React Foundation file is missing: $RelativePath"
    }
}

function Read-Source([string]$RelativePath) {
    Assert-File $RelativePath
    return [System.IO.File]::ReadAllText((Join-Path $root $RelativePath))
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required React Foundation marker '$Needle'."
    }
}

function Reject-Text([string]$RelativePath, [string]$Needle, [string]$Reason) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Reason Found '$Needle' in '$RelativePath'."
    }
}

$requiredFiles = @(
    "packages/react/package.json",
    "packages/react/tsconfig.json",
    "packages/react/src/internal/IdentityContext.ts",
    "packages/react/src/providers/IdentityProvider.tsx",
    "packages/react/src/providers/index.ts",
    "packages/react/src/hooks/useIdentityContextValue.ts",
    "packages/react/src/hooks/useIdentityClient.ts",
    "packages/react/src/hooks/useAuthorization.ts",
    "packages/react/src/hooks/useCapability.ts",
    "packages/react/src/hooks/index.ts",
    "packages/react/src/authorization/RequireCapability.tsx",
    "packages/react/src/authorization/index.ts",
    "packages/react/src/index.ts",
    "packages/react/test/consumer-react.tsx",
    "packages/react/README.md",
    "docs/shared-identity/REACT_FOUNDATION.md",
    "docs/shared-identity/validation-manifests/REACT_FOUNDATION.md"
)
foreach ($relativePath in $requiredFiles) {
    Assert-File $relativePath
}

$package = Get-Content (Join-Path $root "packages/react/package.json") -Raw | ConvertFrom-Json
if ($package.name -ne "@generic-identity/react") {
    throw "React Foundation React package name must remain '@generic-identity/react'."
}
try {
    $reactPackageVersion = [System.Version]$package.version
}
catch {
    throw "React Foundation React package version must be a valid numeric semantic version."
}
if ($reactPackageVersion -lt [System.Version]"0.1.0") {
    throw "React Foundation React package version must not regress below 0.1.0."
}
if ($package.private -ne $true) {
    throw "React Foundation React package must remain private until release qualification."
}
if ($package.type -ne "module") {
    throw "React Foundation React package must remain ESM."
}

$contractsDependency = $package.dependencies.PSObject.Properties['@generic-identity/contracts']
if ($null -eq $contractsDependency -or $contractsDependency.Value -ne "file:../contracts") {
    throw "React Foundation React must depend on the local @generic-identity/contracts boundary."
}
$authDependency = $package.dependencies.PSObject.Properties['@generic-identity/auth']
if ($null -eq $authDependency -or $authDependency.Value -ne "file:../auth") {
    throw "React Foundation React must depend on the local @generic-identity/auth boundary."
}
if ($null -ne $package.dependencies.PSObject.Properties['next']) {
    throw "React Foundation React must not depend on Next.js."
}
if ($null -ne $package.dependencies.PSObject.Properties['@identity-access/client']) {
    throw "React Foundation React must not depend directly on the temporary legacy client bridge."
}

$reactPeer = $package.peerDependencies.PSObject.Properties['react']
$reactDomPeer = $package.peerDependencies.PSObject.Properties['react-dom']
if ($null -eq $reactPeer -or $null -eq $reactDomPeer) {
    throw "React Foundation React and React DOM must be peer dependencies."
}
if ($null -ne $package.dependencies.PSObject.Properties['react'] -or $null -ne $package.dependencies.PSObject.Properties['react-dom']) {
    throw "React Foundation must not install React or React DOM as runtime dependencies."
}

$tsconfig = Get-Content (Join-Path $root "packages/react/tsconfig.json") -Raw | ConvertFrom-Json
if ($tsconfig.compilerOptions.strict -ne $true) {
    throw "React Foundation React typecheck must use strict TypeScript mode."
}
if ($tsconfig.compilerOptions.exactOptionalPropertyTypes -ne $true) {
    throw "React Foundation React must preserve exact optional-property semantics."
}
if ($tsconfig.compilerOptions.noEmit -ne $true) {
    throw "React Foundation remains an extraction bridge and must not publish emitted artifacts yet."
}
if ($tsconfig.compilerOptions.jsx -ne "react-jsx") {
    throw "React Foundation React must use the React JSX transform."
}

if ($tsconfig.compilerOptions.module -ne "ESNext" -or $tsconfig.compilerOptions.moduleResolution -ne "Bundler") {
    throw "React Foundation raw-source React package must use ESNext + Bundler resolution for linked consumer compilation."
}

$sourceFiles = @(
    "packages/react/src/internal/IdentityContext.ts",
    "packages/react/src/providers/IdentityProvider.tsx",
    "packages/react/src/hooks/useIdentityContextValue.ts",
    "packages/react/src/hooks/useIdentityClient.ts",
    "packages/react/src/hooks/useAuthorization.ts",
    "packages/react/src/hooks/useCapability.ts",
    "packages/react/src/authorization/RequireCapability.tsx",
    "packages/react/src/index.ts"
)
foreach ($relativePath in $sourceFiles) {
    Reject-Text $relativePath 'from "next' "React Foundation React must remain independent from Next.js."
    Reject-Text $relativePath 'src/IdentityAccess.' "React Foundation React must not import CLR implementation paths."
    Reject-Text $relativePath 'OrganizationDirectory' "React Foundation React must not absorb Organization Directory ownership."
    Reject-Text $relativePath 'OrganisationProfile' "React Foundation React must not absorb OrganisationProfile ownership."
    Reject-Text $relativePath 'localStorage' "React Foundation React does not own browser session persistence."
    Reject-Text $relativePath 'sessionStorage' "React Foundation React does not own browser session persistence."
    Reject-Text $relativePath 'parseTrn' "React Foundation React must not create a local TRN parser."
    Reject-Text $relativePath 'parseTRN' "React Foundation React must not create a local TRN parser."
}

# Next.js App Router/RSC consumers require explicit client boundaries for
# every shared React module that creates/reads context or uses React hooks.
foreach ($clientModule in @(
    "packages/react/src/internal/IdentityContext.ts",
    "packages/react/src/internal/IdentityVisualContext.ts",
    "packages/react/src/providers/IdentityProvider.tsx",
    "packages/react/src/hooks/useIdentityContextValue.ts",
    "packages/react/src/hooks/useIdentityClient.ts",
    "packages/react/src/hooks/useAuthorization.ts",
    "packages/react/src/hooks/useCapability.ts",
    "packages/react/src/hooks/useIdentityComponents.ts",
    "packages/react/src/authorization/RequireCapability.tsx",
    "packages/react/src/components/IdentityButton.tsx",
    "packages/react/src/components/IdentityInput.tsx",
    "packages/react/src/components/IdentityPanel.tsx",
    "packages/react/src/components/IdentityTable.tsx"
)) {
    Require-Text $clientModule '"use client";'
}

Require-Text "packages/react/src/providers/IdentityProvider.tsx" 'authorizationContext = null'
Require-Text "packages/react/src/hooks/useCapability.ts" '.isAllowedRequirement('
Require-Text "packages/react/src/hooks/useCapability.ts" 'status: "denied"'
Require-Text "packages/react/src/hooks/useCapability.ts" 'status: "error"'
Require-Text "packages/react/src/authorization/RequireCapability.tsx" 'const state = useCapability(requirement)'
Require-Text "packages/react/src/authorization/RequireCapability.tsx" 'throw state.error'
Require-Text "packages/react/src/authorization/RequireCapability.tsx" 'Protected backend operations must still'
Require-Text "packages/react/src/index.ts" 'export * from "./authorization/index"'
Require-Text "packages/react/src/index.ts" 'export * from "./hooks/index"'
Require-Text "packages/react/src/index.ts" 'export * from "./providers/index"'

$probe = "packages/react/test/consumer-react.tsx"
Require-Text $probe 'from "@generic-identity/react"'
Require-Text $probe 'from "@generic-identity/auth"'
Require-Text $probe 'from "@generic-identity/contracts"'
Reject-Text $probe 'clients/typescript' "The React Foundation consumer probe must compile only against public package names."
Reject-Text $probe 'examples/nextjs' "The React Foundation consumer probe must remain independent from the existing host."

# The proven host is deliberately untouched until the consumer integration milestone.
$hostPackage = Get-Content (Join-Path $root "examples/nextjs/admin/package.json") -Raw | ConvertFrom-Json
if ($hostPackage.dependencies.'@identity-access/client' -ne "file:../../../clients/typescript") {
    throw "React Foundation must not redirect the proven Next.js host away from the legacy client."
}
if ($null -ne $hostPackage.dependencies.PSObject.Properties['@generic-identity/react']) {
    throw "React Foundation must not integrate the existing Next.js host prematurely."
}

Write-Host "Shared Identity React Foundation React foundation source validation: GREEN"
