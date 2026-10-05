[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Account and Directory required file is missing: $RelativePath" }
    return $path
}

function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $text = [System.IO.File]::ReadAllText($path)
    if (-not $text.Contains($Needle)) { throw "Account and Directory file '$RelativePath' is missing marker '$Needle'." }
}

foreach ($file in @(
    'packages/contracts/src/account/index.ts',
    'packages/contracts/src/directory/index.ts',
    'packages/auth/src/account.ts',
    'packages/auth/src/directory.ts',
    'packages/react/src/pages/PasswordPage.tsx',
    'packages/react/src/pages/AuthenticationStepUpPage.tsx',
    'packages/react/src/pages/TenantsPage.tsx',
    'packages/react/src/pages/TenantDetailsPage.tsx',
    'packages/react/src/pages/MembershipsPage.tsx',
    'packages/react/src/pages/MembershipCandidatePanel.tsx',
    'packages/react/src/account/PasswordCredentialForm.tsx',
    'packages/react/src/directory/UserForm.tsx',
    'packages/react/src/directory/TenantForm.tsx',
    'packages/react/src/directory/MembershipForm.tsx',
    'packages/react/src/directory/MembershipCandidateForm.tsx',
    'packages/next/src/account/index.ts',
    'packages/next/src/directory/index.ts',
    'docs/shared-identity/full-sdk-feature-matrix.json'
)) { Require-File $file | Out-Null }

Require-Text 'packages/auth/src/client.ts' 'readonly account: GenericIdentityAccountClient;'
Require-Text 'packages/auth/src/client.ts' 'readonly directory: GenericIdentityDirectoryClient;'
Require-Text 'packages/auth/src/directory.ts' 'GenericIdentityDirectoryMembershipCandidatesClient'
Require-Text 'packages/auth/src/account.ts' 'GenericIdentityPasswordCredentialsClient'
Require-Text 'packages/react/src/pages/index.ts' 'TenantsPage'
Require-Text 'packages/react/src/directory/index.ts' 'UserForm'
Require-Text 'packages/react/src/account/index.ts' 'PasswordCredentialForm'
Require-Text 'packages/next/src/pages/index.ts' 'TenantDetailsPage'

$versions = @{}
foreach ($packageName in @('contracts', 'auth', 'react', 'next')) {
    $manifest = Get-Content (Join-Path $root "packages/$packageName/package.json") -Raw | ConvertFrom-Json
    $versions[$packageName] = [string]$manifest.version
    if ([version]$versions[$packageName] -lt [version]'1.1.0') { throw "Account and Directory package '$packageName' must remain at or above version 1.1.0." }
}

Write-Host 'Shared Identity Account and Directory SDK/UI source validation: GREEN'
