[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Groups parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Needle)) { throw "Groups parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.Contains($Needle)) { throw "Groups parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

foreach ($serverModule in @(
    'packages/next/src/server/group-workspace.ts',
    'packages/next/src/server/group-mutations.ts'
)) {
    Require-Text $serverModule 'import "server-only";'
}

foreach ($marker in @(
    'loadNextGroupWorkspace',
    'NextGroupWorkspacePermissions',
    'tenantView',
    'listTemplates',
    'listTemplateScopeRequirements',
    'managedPolicyBindings',
    'group-membership',
    'policy-binding'
)) { Require-Text 'packages/next/src/server/group-workspace.ts' $marker }

foreach ($marker in @(
    'createNextGroupFromForm',
    'updateNextGroupFromForm',
    'updateNextReusableGroupFromForm',
    'createNextGroupFromTemplateFromForm',
    'addNextGroupMemberFromForm',
    'removeNextGroupMemberFromForm',
    'addNextManagedGroupPolicyBindingFromForm',
    'removeNextManagedGroupPolicyBindingFromForm',
    'requireLiteralConfirmation',
    'defaultVersion',
    'resourceScopeMappings'
)) { Require-Text 'packages/next/src/server/group-mutations.ts' $marker }

foreach ($marker in @(
    'loadNextGroupWorkspace',
    'createNextGroupFromTemplateFromForm',
    'addNextManagedGroupPolicyBindingFromForm'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

Require-Text 'packages/react/src/access-control/GroupMemberForm.tsx' 'IdentityEntityAutocomplete'
Require-Text 'packages/react/src/access-control/GroupMemberForm.tsx' 'kind="tenant-membership"'
Reject-Text 'packages/react/src/access-control/GroupMemberForm.tsx' 'Tenant membership ID'
Require-Text 'packages/react/src/access-control/ManagedPolicyBindingForm.tsx' 'kind="managed-policy"'
Require-Text 'packages/react/src/access-control/ManagedPolicyBindingForm.tsx' 'kind="resource-scope"'
Reject-Text 'packages/react/src/access-control/ManagedPolicyBindingForm.tsx' 'Policy version'
Require-Text 'packages/react/src/access-control/GroupFromTemplateForm.tsx' 'resourceScopeMapping:'
Require-Text 'packages/react/src/access-control/index.ts' 'GroupFromTemplateForm'
Require-Text 'packages/react/src/pages/GroupsPage.tsx' 'renderGroupContext'
Require-Text 'packages/react/src/pages/GroupAccessPage.tsx' 'renderMemberActions'
Require-Text 'packages/react/src/pages/GroupAccessPage.tsx' 'renderPolicyBindingActions'

$testPath = Require-File 'packages/next/test/group-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'Groups SDK behavior tests failed.' }

Write-Host 'Generic Identity Groups GOLDEN parity source/behavior validation: GREEN'
