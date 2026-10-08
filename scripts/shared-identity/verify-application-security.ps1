[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Application Security required file is missing: $RelativePath"
    }
    return $path
}

function Read-Source([string]$RelativePath) {
    return [System.IO.File]::ReadAllText((Require-File $RelativePath))
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    if (-not (Read-Source $RelativePath).Contains($Needle)) {
        throw "Application Security file '$RelativePath' is missing marker '$Needle'."
    }
}

$categoryFiles = @(
    'packages/contracts/src/application-security/index.ts',
    'packages/contracts/test/consumer-application-security.ts',
    'packages/auth/src/application-security.ts',
    'packages/auth/test/consumer-application-security.ts',
    'packages/react/src/application-security/index.ts',
    'packages/react/src/application-security/ApplicationSecurityModelsPage.tsx',
    'packages/react/src/application-security/ApplicationSecurityModelDetailsPage.tsx',
    'packages/react/src/application-security/ApplicationCapabilitiesPage.tsx',
    'packages/react/src/application-security/ApplicationScopeTypesPage.tsx',
    'packages/react/src/application-security/ApplicationSecurityContextPage.tsx',
    'packages/react/src/application-security/ApplicationSecurityManifestForm.tsx',
    'packages/react/src/application-security/ApplicationSecurityManifestUploadForm.tsx',
    'packages/next/src/server/security-manifest-parser.ts',
    'packages/next/src/server/application-security-mutations.ts',
    'packages/next/test/security-manifest-parser.test.mjs',
    'packages/react/src/application-security/ApplicationScopeTypeForm.tsx',
    'packages/react/test/consumer-application-security.tsx',
    'packages/next/src/application-security/index.ts',
    'packages/next/test/consumer-application-security.tsx',
    'docs/shared-identity/APPLICATION_SECURITY.md',
    'docs/shared-identity/changes/1.4.0-application-security.md'
)

foreach ($file in $categoryFiles) {
    [void](Require-File $file)
}

Require-Text 'packages/auth/src/client.ts' 'readonly applicationSecurity: GenericIdentityApplicationSecurityClient;'
Require-Text 'packages/auth/src/client.ts' 'createGenericIdentityApplicationSecurityClient('
Require-Text 'packages/auth/src/application-security.ts' 'readonly models: GenericIdentityApplicationSecurityModelsClient;'
Require-Text 'packages/auth/src/application-security.ts' 'readonly manifests: GenericIdentityApplicationSecurityManifestsClient;'
Require-Text 'packages/auth/src/application-security.ts' 'readonly scopeTypes: GenericIdentityApplicationSecurityScopeTypesClient;'
Require-Text 'packages/auth/src/application-security.ts' 'readonly capabilities: GenericIdentityApplicationCapabilitiesClient;'
Require-Text 'packages/auth/src/application-security.ts' 'readonly context: GenericIdentityApplicationSecurityContextClient;'
Require-Text 'packages/auth/src/application-security.ts' 'createApplicationSecurityPermissionReference'
Require-Text 'packages/contracts/src/application-security/index.ts' 'IdentityApplicationSecurityPermissionReference'

Require-Text 'packages/next/src/server/index.ts' 'registerNextApplicationSecurityManifestFromForm'
Require-Text 'packages/next/src/server/application-security-mutations.ts' 'addNextApplicationScopeTypeFromForm'
Require-Text 'packages/next/src/server/security-manifest-parser.ts' 'MAX_SECURITY_MANIFEST_BYTES'

Require-Text 'packages/contracts/package.json' '"./application-security"'
Require-Text 'packages/auth/package.json' '"./application-security"'
Require-Text 'packages/react/package.json' '"./application-security"'
Require-Text 'packages/next/package.json' '"./application-security"'

Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'GenericIdentityApplicationSecurityClient.models list/get + manifests.register'
Require-Text 'docs/shared-identity/FULL_SDK_FEATURE_MATRIX.md' 'IdentityApplicationSecurityPermissionReference + createApplicationSecurityPermissionReference'
Require-Text 'docs/shared-identity/APPLICATION_SECURITY.md' 'does not expose a public helper that manufactures TRN strings'

$versions = @()
foreach ($packageName in @('contracts', 'auth', 'react', 'next')) {
    $manifestPath = Join-Path $root "packages/$packageName/package.json"
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $version = [version][string]$manifest.version
    if ($version -lt [version]'1.4.0') {
        throw "Application Security package '$packageName' must remain at or above version 1.4.0."
    }
    $versions += $version.ToString()
}

$alignedVersions = @($versions | Sort-Object -Unique)
if ($alignedVersions.Count -ne 1) {
    throw 'Generic Identity public package versions must remain aligned.'
}

foreach ($file in $categoryFiles) {
    $source = [System.IO.File]::ReadAllText((Join-Path $root $file))
    if ($source.IndexOf('consumer-app', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Consumer-neutral Application Security source '$file' must not contain a consumer product name."
    }
}

$changeRecord = Read-Source 'docs/shared-identity/changes/1.4.0-application-security.md'
foreach ($marker in @('## Added', '## Modified', '## Moved', '## Deleted', '## Database migrations', '## Runtime changes', '## Breaking changes', '## Post-apply validation')) {
    if (-not $changeRecord.Contains($marker)) {
        throw "Application Security change record is missing section '$marker'."
    }
}

Write-Host 'Generic Identity Application Security source validation: GREEN'
