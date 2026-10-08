[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Resource Scopes parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $source = [System.IO.File]::ReadAllText((Require-File $RelativePath))
    if (-not $source.Contains($Needle)) { throw "Resource Scopes parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $source = [System.IO.File]::ReadAllText((Require-File $RelativePath))
    if ($source.Contains($Needle)) { throw "Resource Scopes parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

foreach ($serverModule in @(
    'packages/next/src/server/resource-scope-workspace.ts',
    'packages/next/src/server/resource-scope-mutations.ts'
)) { Require-Text $serverModule 'import "server-only";' }

foreach ($marker in @(
    'loadNextResourceScopeWorkspace',
    'NextResourceScopeWorkspacePermissions',
    'tenantView',
    'aggregateScopes',
    'resource-scope',
    'scope-type',
    'parentResourceScope'
)) { Require-Text 'packages/next/src/server/resource-scope-workspace.ts' $marker }

foreach ($marker in @(
    'createNextResourceScopeFromForm',
    'updateNextResourceScopeFromForm',
    'selected scope type is not registered',
    'a resource scope cannot be its own parent',
    'expectedVersion',
    'parentResourceScopeId'
)) { Require-Text 'packages/next/src/server/resource-scope-mutations.ts' $marker }

foreach ($marker in @(
    'loadNextResourceScopeWorkspace',
    'createNextResourceScopeFromForm',
    'updateNextResourceScopeFromForm'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

Require-Text 'packages/react/src/access-control/ResourceScopeForm.tsx' 'ResourceScopeTypeFields'
Require-Text 'packages/react/src/access-control/ResourceScopeForm.tsx' 'IdentityEntityAutocomplete'
Require-Text 'packages/react/src/access-control/ResourceScopeForm.tsx' 'kind="resource-scope"'
Require-Text 'packages/react/src/access-control/ResourceScopeForm.tsx' 'includeInactive'
Reject-Text 'packages/react/src/access-control/ResourceScopeForm.tsx' '<IdentityInput name="scopeType"'
Require-Text 'packages/react/src/access-control/ResourceScopeTypeFields.tsx' '/api/identity/scope-types'
Require-Text 'packages/react/src/pages/ResourceScopesPage.tsx' 'renderScopeContext'
Require-Text 'packages/react/src/pages/ResourceScopeDetailsPage.tsx' 'Record version'
Require-Text 'packages/react/src/components/IdentityEntityAutocomplete.tsx' 'includeInactive'

$testPath = Require-File 'packages/next/test/resource-scope-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'Resource Scopes SDK behavior tests failed.' }

Write-Host 'Generic Identity Resource Scopes GOLDEN parity source/behavior validation: GREEN'
