[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Require-File([string]$RelativePath) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) { throw "Delegated Authority parity required file is missing: $RelativePath" }
    return $path
}
function Require-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if (-not $source.Contains($Needle)) { throw "Delegated Authority parity file '$RelativePath' is missing marker '$Needle'." }
}
function Reject-Text([string]$RelativePath, [string]$Needle) {
    $path = Require-File $RelativePath
    $source = [System.IO.File]::ReadAllText($path)
    if ($source.Contains($Needle)) { throw "Delegated Authority parity file '$RelativePath' contains forbidden marker '$Needle'." }
}

foreach ($serverModule in @(
    'packages/next/src/server/delegated-authority-workspace.ts',
    'packages/next/src/server/delegated-authority-mutations.ts'
)) {
    Require-Text $serverModule 'import "server-only";'
}

foreach ($marker in @(
    'loadNextDelegatedAuthorityWorkspace',
    'NextDelegatedAuthorityWorkspacePermissions',
    'scope-authority-group',
    'scope-authority-membership',
    'scope-authority-policy',
    'scope-authority-statement',
    'scope-authority-binding',
    'listMembers',
    'listPolicyStatements',
    'listPolicyBindings',
    'memberUsers',
    'boundPolicies'
)) { Require-Text 'packages/next/src/server/delegated-authority-workspace.ts' $marker }

foreach ($marker in @(
    'createNextScopeAuthorityGroupFromForm',
    'updateNextScopeAuthorityGroupFromForm',
    'createNextScopeAuthorityPolicyFromForm',
    'updateNextScopeAuthorityPolicyFromForm',
    'addNextScopeAuthorityMemberFromForm',
    'removeNextScopeAuthorityMemberFromForm',
    'addNextScopeAuthorityPolicyStatementFromForm',
    'removeNextScopeAuthorityPolicyStatementFromForm',
    'addNextScopeAuthorityPolicyBindingFromForm',
    'removeNextScopeAuthorityPolicyBindingFromForm',
    'requireLiteralConfirmation'
)) { Require-Text 'packages/next/src/server/delegated-authority-mutations.ts' $marker }

foreach ($marker in @(
    'loadNextDelegatedAuthorityWorkspace',
    'createNextScopeAuthorityGroupFromForm',
    'addNextScopeAuthorityPolicyBindingFromForm'
)) { Require-Text 'packages/next/src/server/index.ts' $marker }

Require-Text 'packages/react/src/access-control/DelegatedAuthorityMemberForm.tsx' 'IdentityEntityAutocomplete'
Require-Text 'packages/react/src/access-control/DelegatedAuthorityMemberForm.tsx' 'kind="user"'
Reject-Text 'packages/react/src/access-control/DelegatedAuthorityMemberForm.tsx' 'User ID</span>'

Require-Text 'packages/react/src/access-control/DelegatedAuthorityPolicyBindingForm.tsx' 'IdentityEntityAutocomplete'
Require-Text 'packages/react/src/access-control/DelegatedAuthorityPolicyBindingForm.tsx' 'kind="authority-policy"'
Reject-Text 'packages/react/src/access-control/DelegatedAuthorityPolicyBindingForm.tsx' '<select'

Require-Text 'packages/react/src/pages/DelegatedAuthorityPage.tsx' 'Authority group members'
Require-Text 'packages/react/src/pages/DelegatedAuthorityPage.tsx' 'Authority policy bindings'
Require-Text 'packages/react/src/pages/DelegatedAuthorityPage.tsx' 'Authority policy statements'
Require-Text 'packages/react/src/pages/DelegatedAuthorityPage.tsx' 'Tenant ownership'
# Keep verification ASCII-only for Windows PowerShell 5.1 compatibility.
# The rendered source may use typographic punctuation, but the semantic markers
# below are stable and avoid UTF-8-without-BOM mojibake during script parsing.
Require-Text 'packages/react/src/pages/DelegatedAuthorityPage.tsx' 'None'
Require-Text 'packages/react/src/pages/DelegatedAuthorityPage.tsx' 'identity scope'

$testPath = Require-File 'packages/next/test/delegated-authority-workflows.behavior.test.mjs'
& node --test $testPath
if ($LASTEXITCODE -ne 0) { throw 'Delegated Authority SDK behavior tests failed.' }

Write-Host 'Generic Identity Delegated Authority GOLDEN parity source/behavior validation: GREEN'
