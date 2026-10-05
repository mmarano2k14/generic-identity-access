[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Next.js Integration file is missing: $RelativePath"
    }
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    Require-File $RelativePath
    $text = [System.IO.File]::ReadAllText((Join-Path $root $RelativePath))
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required Next.js Integration marker '$Needle'."
    }
}

$requiredFiles = @(
    "packages/next/package.json",
    "packages/next/tsconfig.json",
    "packages/next/src/index.ts",
    "packages/next/src/client/NextIdentityProvider.tsx",
    "packages/next/src/client/index.ts",
    "packages/next/src/pages/index.ts",
    "packages/next/src/routing/routes.ts",
    "packages/next/src/routing/index.ts",
    "packages/next/src/server/config.ts",
    "packages/next/src/server/session.ts",
    "packages/next/src/server/authorization.ts",
    "packages/next/src/server/protected-page.ts",
    "packages/next/src/server/index.ts",
    "packages/next/test/consumer-next.tsx",
    "packages/next/test/consumer-server.ts",
    "docs/shared-identity/NEXTJS_INTEGRATION.md",
    "docs/shared-identity/validation-manifests/NEXTJS_INTEGRATION.md"
)
foreach ($relativePath in $requiredFiles) {
    Require-File $relativePath
}

$package = [System.IO.File]::ReadAllText((Join-Path $root "packages/next/package.json")) | ConvertFrom-Json
if ($package.name -ne "@generic-identity/next") {
    throw "Next.js Integration package name must remain @generic-identity/next."
}
try {
    $packageVersion = [System.Version]$package.version
}
catch {
    throw "Next.js Integration Next package version must be a valid numeric semantic version."
}
if ($packageVersion -lt [System.Version]"0.1.0") {
    throw "Next.js Integration Next package version must not regress below 0.1.0."
}
if ($package.private -ne $true) {
    throw "Next.js Integration Next package must remain private until release qualification."
}
if ($package.type -ne "module") {
    throw "Next.js Integration Next package must remain ESM."
}

foreach ($dependency in @(
    "@generic-identity/auth",
    "@generic-identity/contracts",
    "@generic-identity/react",
    "server-only"
)) {
    if ($null -eq $package.dependencies.PSObject.Properties[$dependency]) {
        throw "Next.js Integration Next package is missing required dependency '$dependency'."
    }
}
if ($null -ne $package.dependencies.PSObject.Properties['@identity-access/client']) {
    throw "Next.js Integration Next package must not bypass the shared auth boundary to use the legacy client directly."
}
if ($null -ne $package.dependencies.PSObject.Properties['react'] -or $null -ne $package.dependencies.PSObject.Properties['react-dom'] -or $null -ne $package.dependencies.PSObject.Properties['next']) {
    throw "Next.js Integration React, React DOM and Next.js must be peer dependencies, not runtime dependencies."
}
foreach ($peer in @("react", "react-dom", "next")) {
    if ($null -eq $package.peerDependencies.PSObject.Properties[$peer]) {
        throw "Next.js Integration Next package is missing peer dependency '$peer'."
    }
}

$tsconfig = [System.IO.File]::ReadAllText((Join-Path $root "packages/next/tsconfig.json")) | ConvertFrom-Json
if ($tsconfig.compilerOptions.strict -ne $true -or $tsconfig.compilerOptions.exactOptionalPropertyTypes -ne $true -or $tsconfig.compilerOptions.noEmit -ne $true) {
    throw "Next.js Integration Next package must preserve strict, exact optional-property, no-emit TypeScript settings."
}
if ($tsconfig.compilerOptions.jsx -ne "react-jsx") {
    throw "Next.js Integration Next package must use the React JSX transform."
}
if ($tsconfig.compilerOptions.module -ne "ESNext" -or $tsconfig.compilerOptions.moduleResolution -ne "Bundler") {
    throw "Next.js Integration Next package must use ESNext + Bundler resolution so Next.js package subpaths resolve correctly."
}
if ($tsconfig.compilerOptions.skipLibCheck -ne $true) {
    throw "Next.js Integration Next package must keep skipLibCheck enabled so standalone package checks validate Generic Identity source without re-typechecking Next.js declaration internals."
}

Require-Text "packages/next/src/client/NextIdentityProvider.tsx" '"use client"'
Require-Text "packages/next/src/client/NextIdentityProvider.tsx" 'from "@generic-identity/react/providers"'
Require-Text "packages/next/src/client/NextIdentityProvider.tsx" 'from "@generic-identity/react/visual"'
Require-Text "packages/next/src/server/session.ts" 'import "server-only"'
Require-Text "packages/next/src/server/session.ts" "httpOnly: true"
Require-Text "packages/next/src/server/session.ts" 'sameSite: "lax"'
Require-Text "packages/next/src/server/session.ts" "validateSession("
Require-Text "packages/next/src/server/authorization.ts" "createAuthorizationContext("
Require-Text "packages/next/src/server/authorization.ts" "if (!allowed) throw new NextIdentityPermissionDeniedError()"
Require-Text "packages/next/src/server/protected-page.ts" "performs no redirect"
Require-Text "packages/next/src/routing/routes.ts" "Routes remain owned"
Require-Text "packages/next/src/pages/index.ts" 'from "@generic-identity/react/pages"'

$nextSourceRoot = Join-Path $root "packages/next/src"
$sourceFiles = @(Get-ChildItem $nextSourceRoot -Recurse -File | Where-Object { $_.Extension -in @(".ts", ".tsx") })
foreach ($file in $sourceFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text.IndexOf("@identity-access/client", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Next.js Integration Next source must not import the temporary legacy client directly: $($file.FullName)"
    }
    if ($text.IndexOf("consumer-app", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Next.js Integration Generic Identity Next source must remain consumer-agnostic: $($file.FullName)"
    }
    if ($text.IndexOf("localStorage", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or $text.IndexOf("sessionStorage", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Next.js Integration session credentials must remain server-only: $($file.FullName)"
    }
}

$serverFiles = @(Get-ChildItem (Join-Path $nextSourceRoot "server") -File | Where-Object { $_.Extension -eq ".ts" })
foreach ($file in $serverFiles) {
    if ($file.Name -eq "config.ts" -or $file.Name -eq "index.ts") { continue }
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text.IndexOf('import "server-only"', [System.StringComparison]::Ordinal) -lt 0) {
        throw "Next.js Integration server module must declare the server-only boundary: $($file.FullName)"
    }
}

# Consumer migration belongs to Consumer Integration. Next.js Integration must leave the proven host untouched.
$hostPackage = [System.IO.File]::ReadAllText((Join-Path $root "examples/nextjs/admin/package.json")) | ConvertFrom-Json
if ($hostPackage.dependencies.'@identity-access/client' -ne "file:../../../clients/typescript") {
    throw "Next.js Integration must not redirect the proven Next.js host away from the existing client yet."
}
if ($null -ne $hostPackage.dependencies.PSObject.Properties['@generic-identity/next']) {
    throw "Next.js Integration must not switch the existing host to @generic-identity/next before the consumer-integration milestone."
}
foreach ($legacyPage in @(
    "examples/nextjs/admin/app/login/page.tsx",
    "examples/nextjs/admin/app/recovery/page.tsx",
    "examples/nextjs/admin/app/identity/users/page.tsx",
    "examples/nextjs/admin/app/identity/groups/page.tsx",
    "examples/nextjs/admin/app/identity/policies/page.tsx",
    "examples/nextjs/admin/app/identity/sessions/page.tsx",
    "examples/nextjs/admin/app/identity/mfa/page.tsx"
)) {
    Require-File $legacyPage
}

$changelog = [System.IO.File]::ReadAllText((Join-Path $root "CHANGELOG.md"))
foreach ($milestoneName in @(
    "Baseline and Structure",
    "Public Contracts",
    "Authentication and Authorization SDK",
    "React Foundation",
    "Shared React Pages",
    "Theme and Component Overrides",
    "Next.js Integration"
)) {
    if ($changelog.IndexOf($milestoneName, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "CHANGELOG.md is missing Shared Identity history for '$milestoneName'."
    }
}

$manifest = [System.IO.File]::ReadAllText((Join-Path $root "docs/shared-identity/validation-manifests/NEXTJS_INTEGRATION.md"))
foreach ($marker in @("## MOVED", "NONE", "## DELETED", "## DELETE AFTER VALIDATION", "NONE IN Next.js Integration")) {
    if ($manifest.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Next.js Integration manifest must explicitly document move/delete state. Missing '$marker'."
    }
}

Write-Host "Shared Identity Next.js integration source validation: GREEN"
