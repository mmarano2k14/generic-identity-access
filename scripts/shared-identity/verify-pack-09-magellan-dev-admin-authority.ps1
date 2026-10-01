[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptPath = Join-Path $root 'scripts\authentication\grant-magellan-dev-admin.ps1'
if (-not (Test-Path $scriptPath -PathType Leaf)) {
    throw 'MAGELLAN development administrator authority bootstrap script is missing.'
}

$content = Get-Content $scriptPath -Raw
$required = @(
    "[string]`$ApplicationKey = 'magellan'",
    "'identity-access', '*', '*'",
    'application_security_models',
    'application_security_model_registrations',
    'manifest_sha256',
    'rbac_project',
    'application_security_namespaces',
    'application_capabilities',
    'identity_scope_administration_group_memberships',
    'identity_scope_administration_group_policy_bindings',
    'ON CONFLICT DO NOTHING',
    'MAGELLAN local Generic Identity administration authority: GREEN'
)
foreach ($marker in $required) {
    if (-not $content.Contains($marker)) {
        throw "MAGELLAN development administrator authority bootstrap is missing required marker '$marker'."
    }
}

if ($content.Contains('DELETE FROM')) {
    throw 'MAGELLAN development administrator authority bootstrap must remain additive and may not delete authority state.'
}

Write-Host 'Shared Identity Pack 9 MAGELLAN development administrator authority source validation: GREEN'
