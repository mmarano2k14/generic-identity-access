[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Assert-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Shared React Pages file is missing: $RelativePath"
    }
}

function Read-Source([string]$RelativePath) {
    Assert-File $RelativePath
    return [System.IO.File]::ReadAllText((Join-Path $root $RelativePath))
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required Shared React Pages marker '$Needle'."
    }
}

function Reject-Text([string]$RelativePath, [string]$Needle, [string]$Reason) {
    $text = Read-Source $RelativePath
    if ($text.IndexOf($Needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Reason Found '$Needle' in '$RelativePath'."
    }
}

$requiredFiles = @(
    "packages/react/src/components/IdentityEmptyState.tsx",
    "packages/react/src/components/IdentityPageFrame.tsx",
    "packages/react/src/components/IdentityPanel.tsx",
    "packages/react/src/components/IdentityStatus.tsx",
    "packages/react/src/components/IdentityTable.tsx",
    "packages/react/src/components/index.ts",
    "packages/react/src/pages/AccountPage.tsx",
    "packages/react/src/pages/GroupDetailsPage.tsx",
    "packages/react/src/pages/GroupsPage.tsx",
    "packages/react/src/pages/MfaPage.tsx",
    "packages/react/src/pages/PoliciesPage.tsx",
    "packages/react/src/pages/PolicyDetailsPage.tsx",
    "packages/react/src/pages/ProfilePage.tsx",
    "packages/react/src/pages/RecoveryPage.tsx",
    "packages/react/src/pages/SecurityPage.tsx",
    "packages/react/src/pages/SessionsPage.tsx",
    "packages/react/src/pages/SignInPage.tsx",
    "packages/react/src/pages/UserDetailsPage.tsx",
    "packages/react/src/pages/UsersPage.tsx",
    "packages/react/src/pages/index.ts",
    "packages/react/src/pages/internal.tsx",
    "packages/react/test/consumer-pages.tsx",
    "docs/shared-identity/SHARED_PAGES.md",
    "docs/shared-identity/validation-manifests/SHARED_PAGES.md"
)
foreach ($relativePath in $requiredFiles) {
    Assert-File $relativePath
}

$package = Get-Content (Join-Path $root "packages/react/package.json") -Raw | ConvertFrom-Json
if ($package.name -ne "@generic-identity/react") {
    throw "Shared React Pages must extend the existing @generic-identity/react package."
}
try {
    $reactPackageVersion = [System.Version]$package.version
}
catch {
    throw "Shared React Pages React package version must be a valid numeric semantic version."
}
if ($reactPackageVersion -lt [System.Version]"0.1.0") {
    throw "Shared React Pages React package version must not regress below 0.1.0."
}
if ($null -eq $package.exports.PSObject.Properties['./components']) {
    throw "Shared React Pages must expose the shared components boundary."
}
if ($null -eq $package.exports.PSObject.Properties['./pages']) {
    throw "Shared React Pages must expose the shared pages boundary."
}
if ($null -ne $package.dependencies.PSObject.Properties['next']) {
    throw "Shared React Pages React pages must remain independent from Next.js."
}
if ($null -ne $package.dependencies.PSObject.Properties['@identity-access/client']) {
    throw "Shared React Pages React pages must not bypass @generic-identity/auth/contracts to use the legacy client directly."
}

$pageFiles = Get-ChildItem (Join-Path $root "packages/react/src/pages") -File | Where-Object { $_.Extension -eq ".ts" -or $_.Extension -eq ".tsx" }
$componentFiles = Get-ChildItem (Join-Path $root "packages/react/src/components") -File | Where-Object { $_.Extension -eq ".ts" -or $_.Extension -eq ".tsx" }
foreach ($file in @($pageFiles) + @($componentFiles)) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $relativePath = $file.FullName.Substring($root.Length).Replace('\','/')
    if ($relativePath.StartsWith("/")) { $relativePath = $relativePath.Substring(1) }
    foreach ($needle in @('from "next', "from 'next", '@identity-access/client', 'clients/typescript', 'src/IdentityAccess.', 'OrganizationDirectory', 'OrganisationProfile', 'localStorage', 'sessionStorage', 'parseTrn', 'parseTRN')) {
        if ($text.IndexOf($needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Shared React surface crossed an ownership boundary. Found '$needle' in '$relativePath'."
        }
    }
    if ($text -match 'style\s*=\s*\{\{') {
        throw "Shared React pages must use the stable semantic class surface, not inline React style objects. Found in '$relativePath'."
    }
}

$index = "packages/react/src/index.ts"
Require-Text $index 'export * from "./components/index"'
Require-Text $index 'export * from "./pages/index"'

$pagesIndex = "packages/react/src/pages/index.ts"
foreach ($export in @(
    './AccountPage', './GroupDetailsPage', './GroupsPage', './MfaPage',
    './PoliciesPage', './PolicyDetailsPage', './ProfilePage', './RecoveryPage',
    './SecurityPage', './SessionsPage', './SignInPage', './UserDetailsPage', './UsersPage'
)) {
    Require-Text $pagesIndex $export
}

Require-Text "packages/react/src/pages/SignInPage.tsx" 'method="post"'
Require-Text "packages/react/src/pages/SignInPage.tsx" 'autoComplete="current-password"'
Require-Text "packages/react/src/pages/RecoveryPage.tsx" 'autoComplete="one-time-code"'
Require-Text "packages/react/src/pages/SessionsPage.tsx" 'Session credentials are never rendered.'
Require-Text "packages/react/src/pages/MfaPage.tsx" 'Provider secrets are never rendered.'
Require-Text "packages/react/src/components/IdentityPageFrame.tsx" 'data-gi-component="page"'
Require-Text "packages/react/src/components/IdentityStatus.tsx" 'data-gi-tone={tone}'

$probe = "packages/react/test/consumer-pages.tsx"
Require-Text $probe 'from "@generic-identity/react"'
Require-Text $probe 'from "@generic-identity/contracts"'
Reject-Text $probe 'examples/nextjs' "The Shared React Pages consumer probe must remain independent from the proven host."
Reject-Text $probe 'clients/typescript' "The Shared React Pages consumer probe must compile only against public package boundaries."

# Shared React Pages is additive. The proven host remains untouched until the Next.js/consumer integration milestones.
$hostPackage = Get-Content (Join-Path $root "examples/nextjs/admin/package.json") -Raw | ConvertFrom-Json
if ($hostPackage.dependencies.'@identity-access/client' -ne "file:../../../clients/typescript") {
    throw "Shared React Pages must not redirect the existing Next.js host away from the proven legacy client."
}
if ($null -ne $hostPackage.dependencies.PSObject.Properties['@generic-identity/react']) {
    throw "Shared React Pages must not integrate the existing Next.js host prematurely."
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
    Assert-File $legacyPage
}

$manifest = Read-Source "docs/shared-identity/validation-manifests/SHARED_PAGES.md"
foreach ($marker in @('## MOVED', 'NONE', '## DELETED', '## DELETE AFTER VALIDATION', 'NONE IN Shared React Pages')) {
    if ($manifest.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Shared React Pages manifest must explicitly document move/delete state. Missing '$marker'."
    }
}

Write-Host "Shared Identity shared React pages source validation: GREEN"
