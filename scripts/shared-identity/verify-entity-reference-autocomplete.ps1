[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

function Require-Marker([string]$RelativePath, [string]$Marker) {
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Entity-reference autocomplete file is missing: $RelativePath"
    }
    $text = [System.IO.File]::ReadAllText($path)
    if ($text.IndexOf($Marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Entity-reference autocomplete file '$RelativePath' is missing '$Marker'."
    }
}

Require-Marker 'packages/contracts/src/entity-references.ts' 'IdentityEntityReferenceKind'
Require-Marker 'packages/contracts/src/entity-references.ts' 'IdentityEntityReferenceOption'
Require-Marker 'packages/react/src/components/IdentityEntityAutocomplete.tsx' 'IDENTITY_ENTITY_AUTOCOMPLETE_MINIMUM_SEARCH_LENGTH = 3'
Require-Marker 'packages/react/src/components/IdentityEntityAutocomplete.tsx' 'IDENTITY_ENTITY_AUTOCOMPLETE_DEBOUNCE_MS = 250'
Require-Marker 'packages/react/src/components/IdentityEntityAutocomplete.tsx' 'AbortController'
Require-Marker 'packages/react/src/components/IdentityEntityAutocomplete.tsx' 'slice(0, IDENTITY_ENTITY_AUTOCOMPLETE_MAXIMUM_RESULTS)'
Require-Marker 'packages/react/src/components/IdentityEntityAutocomplete.tsx' 'type="hidden" name={name} value={selectedId}'
Require-Marker 'packages/react/src/directory/MembershipForm.tsx' 'kind="user"'
Require-Marker 'packages/next/src/server/entity-references.ts' 'searchNextIdentityEntityReferences'
Require-Marker 'packages/next/src/server/entity-references.ts' 'NEXT_IDENTITY_ENTITY_REFERENCE_MAXIMUM_RESULTS = 20'
Require-Marker 'packages/next/src/server/entity-references.ts' 'tenant_context_outside_visibility'
Require-Marker 'packages/next/src/server/index.ts' './entity-references'
Require-Marker 'packages/next/src/server/membership-workspace.ts' 'no tenant catalog is preloaded'

Write-Host 'Generic Identity server-backed entity-reference autocomplete source validation: GREEN'
