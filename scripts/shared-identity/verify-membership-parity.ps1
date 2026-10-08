[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Require-Marker([string]$RelativePath, [string]$Marker) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Membership SDK file is missing: $RelativePath"
    }
    $text = [System.IO.File]::ReadAllText($path)
    if ($text.IndexOf($Marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Membership SDK file '$RelativePath' is missing '$Marker'."
    }
}

Require-Marker 'packages/next/src/server/membership-workspace.ts' 'loadNextMembershipWorkspace'
Require-Marker 'packages/next/src/server/membership-workspace.ts' 'isAllowedOnServer'
Require-Marker 'packages/next/src/server/membership-mutations.ts' 'createNextTenantMembershipFromForm'
Require-Marker 'packages/next/src/server/membership-mutations.ts' 'replaceNextTenantMemberOrganizationsFromForm'
Require-Marker 'packages/next/src/server/index.ts' 'replaceNextTenantMemberGroupsFromForm'
Require-Marker 'packages/react/src/directory/MembershipForm.tsx' 'typeof formAction === "function"'
Require-Marker 'packages/react/src/directory/MembershipCandidateForm.tsx' 'typeof formAction === "function"'
Require-Marker 'packages/react/src/directory/MembershipCandidateLookupPanel.tsx' 'useActionState'
Require-Marker 'packages/react/src/directory/MemberGroupAssignmentsForm.tsx' 'groupSelection'
Require-Marker 'packages/react/src/directory/MemberOrganizationAssignmentsForm.tsx' 'organizationSelection'

Write-Host 'Membership SDK source wiring: GREEN'
& node (Join-Path $root 'packages/next/test/membership-workflows.behavior.test.mjs')
if ($LASTEXITCODE -ne 0) { throw 'Membership SDK behavior tests failed.' }
Write-Host 'Membership SDK behavior tests: GREEN'
