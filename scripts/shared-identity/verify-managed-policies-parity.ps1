[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Managed Policies parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $source = [System.IO.File]::ReadAllText((Require-File $RelativePath))
    if (-not $source.Contains($Needle)) { throw "Managed Policies parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $source = [System.IO.File]::ReadAllText((Require-File $RelativePath))
    if ($source.Contains($Needle)) { throw "Managed Policies parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

foreach ($serverModule in @(
    'packages/next/src/server/managed-policy-workspace.ts',
    'packages/next/src/server/managed-policy-mutations.ts'
)) { Require-Text $serverModule 'import "server-only";' }

foreach ($marker in @(
    'loadNextManagedPolicyWorkspace',
    'NextManagedPolicyWorkspacePermissions',
    'policy-statement',
    'security-model',
    'policyBuilderModels',
    'trnPreviews',
    'policiesTruncated'
)) { Require-Text 'packages/next/src/server/managed-policy-workspace.ts' $marker }

foreach ($marker in @(
    'createNextManagedPolicyFromForm',
    'updateNextManagedPolicyFromForm',
    'createNextManagedPolicyVersionFromForm',
    'publishNextManagedPolicyVersionFromForm',
    'addNextManagedPolicyStatementFromForm',
    'removeNextManagedPolicyStatementFromForm',
    'published policy versions are immutable',
    'capability does not belong to the policy version security model',
    'confirmation must be'
)) { Require-Text 'packages/next/src/server/managed-policy-mutations.ts' $marker }

foreach ($marker in @(
    'loadNextManagedPolicyWorkspace',
    'createNextManagedPolicyVersionFromForm',
    'addNextManagedPolicyStatementFromForm'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

Require-Text 'packages/react/src/access-control/ManagedPolicyForm.tsx' 'policyId'
Require-Text 'packages/react/src/access-control/ManagedPolicyForm.tsx' 'expectedVersion'
Require-Text 'packages/react/src/access-control/ManagedPolicyForm.tsx' 'typeof formAction === "function"'
Require-Text 'packages/react/src/access-control/ManagedPolicyVersionForm.tsx' 'Select a registered model'
Reject-Text 'packages/react/src/access-control/ManagedPolicyVersionForm.tsx' '<IdentityInput name="modelVersion"'
Require-Text 'packages/react/src/access-control/ManagedPolicyStatementForm.tsx' 'Select a registered capability'
Require-Text 'packages/react/src/access-control/ManagedPolicyStatementForm.tsx' 'name="capability"'
Reject-Text 'packages/react/src/access-control/ManagedPolicyStatementForm.tsx' 'Resource pattern'
Require-Text 'packages/react/src/access-control/ManagedPolicyPublishForm.tsx' 'Make this the default version'
Require-Text 'packages/react/src/access-control/ManagedPolicyStatementRemoveForm.tsx' 'Type REMOVE to confirm'
Require-Text 'packages/react/src/pages/PolicyDetailsPage.tsx' 'renderVersionActions'
Require-Text 'packages/react/src/pages/PolicyDetailsPage.tsx' 'renderStatementActions'
Require-Text 'packages/react/src/access-control/index.ts' 'ManagedPolicyPublishForm'

$testPath = Require-File 'packages/next/test/managed-policy-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'Managed Policies SDK behavior tests failed.' }

Write-Host 'Generic Identity Managed Policies GOLDEN parity source/behavior validation: GREEN'
