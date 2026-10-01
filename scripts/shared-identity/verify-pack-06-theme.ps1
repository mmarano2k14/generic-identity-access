[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required Pack 6 file is missing: $RelativePath"
    }
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    Require-File $RelativePath
    $text = [System.IO.File]::ReadAllText((Join-Path $root $RelativePath))
    if ($text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw "'$RelativePath' is missing required Pack 6 marker '$Needle'."
    }
}

$requiredFiles = @(
    "packages/react/src/internal/IdentityVisualContext.ts",
    "packages/react/src/hooks/useIdentityComponents.ts",
    "packages/react/src/components/IdentityButton.tsx",
    "packages/react/src/components/IdentityInput.tsx",
    "packages/react/src/theme/IdentityThemeRoot.tsx",
    "packages/react/src/theme/tokens.ts",
    "packages/react/src/theme/default.css",
    "packages/react/src/theme/index.ts",
    "packages/react/src/visual/types.ts",
    "packages/react/src/visual/index.ts",
    "packages/react/test/consumer-theme.tsx",
    "docs/shared-identity/PACK_06_THEME_AND_COMPONENT_OVERRIDES.md",
    "docs/shared-identity/packs/PACK_06_THEME_AND_COMPONENT_OVERRIDES_MANIFEST.md"
)

foreach ($relativePath in $requiredFiles) {
    Require-File $relativePath
}

Require-Text "packages/react/src/providers/IdentityProvider.tsx" "components?: IdentityComponentOverrides"
Require-Text "packages/react/src/providers/IdentityProvider.tsx" "IdentityVisualContext.Provider"
Require-Text "packages/react/src/components/IdentityButton.tsx" 'data-gi-component="button"'
Require-Text "packages/react/src/components/IdentityInput.tsx" 'data-gi-component="input"'
Require-Text "packages/react/src/components/IdentityPanel.tsx" "const { Panel } = useIdentityComponents();"
Require-Text "packages/react/src/components/IdentityTable.tsx" "const { Table } = useIdentityComponents();"
Require-Text "packages/react/src/pages/SignInPage.tsx" "<IdentityButton"
Require-Text "packages/react/src/pages/SignInPage.tsx" "<IdentityInput"
Require-Text "packages/react/src/pages/RecoveryPage.tsx" "<IdentityButton"
Require-Text "packages/react/src/pages/RecoveryPage.tsx" "<IdentityInput"
Require-Text "packages/react/src/theme/default.css" "--gi-background:"
Require-Text "packages/react/src/theme/default.css" "--gi-primary:"
Require-Text "packages/react/src/theme/default.css" ".gi-button-primary"
Require-Text "packages/react/src/theme/default.css" ".gi-table-shell"
Require-Text "packages/react/package.json" '"./theme.css"'
$reactPackage = [System.IO.File]::ReadAllText((Join-Path $root "packages/react/package.json")) | ConvertFrom-Json
try {
    $reactPackageVersion = [System.Version]$reactPackage.version
}
catch {
    throw "Pack 6 React package version must be a valid numeric semantic version."
}
if ($reactPackageVersion -lt [System.Version]"0.2.0") {
    throw "Pack 6 React package version must not regress below 0.2.0."
}

$changelogPath = Join-Path $root "CHANGELOG.md"
Require-File "CHANGELOG.md"
$changelogText = [System.IO.File]::ReadAllText($changelogPath)
for ($packNumber = 1; $packNumber -le 6; $packNumber++) {
    $pattern = "(?m)^# Shared Identity Integration .* Pack $packNumber(?: .*|$)"
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($changelogText, $pattern)) {
        throw "CHANGELOG.md is missing the Shared Identity Integration Pack $packNumber entry."
    }
}

$reactSourceRoot = Join-Path $root "packages/react/src"
$sourceFiles = @(Get-ChildItem $reactSourceRoot -Recurse -File | Where-Object { $_.Extension -in @(".ts", ".tsx") })
foreach ($file in $sourceFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text -match 'from\s+["'']next(?:/[^"'']*)?["'']') {
        throw "Pack 6 React boundary must not depend on Next.js: $($file.FullName)"
    }
    if ($text -match '(?i)magellan') {
        throw "Pack 6 Generic Identity React source must remain consumer-agnostic: $($file.FullName)"
    }
}

Write-Host "Shared Identity Pack 6 theme/component override source validation: GREEN"
