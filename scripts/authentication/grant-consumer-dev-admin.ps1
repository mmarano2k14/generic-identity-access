[CmdletBinding()]
param(
    [Guid]$IdentityScopeId = [Guid]'00000000-0000-0000-0000-000000000001',
    [Guid]$UserId = [Guid]'00000000-0000-0000-0000-000000000003',
    [string]$ApplicationKey = 'consumer-app',
    [int]$ModelVersion = 1,
    [string]$SecurityManifestPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw 'psql was not found on PATH.'
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($SecurityManifestPath)) {
    $SecurityManifestPath = Join-Path $root 'config\identity-access-admin-security-manifest.json'
}
$SecurityManifestPath = [IO.Path]::GetFullPath($SecurityManifestPath)
if (-not (Test-Path $SecurityManifestPath -PathType Leaf)) {
    throw "Shared Identity administration security manifest was not found: $SecurityManifestPath"
}
if ($ApplicationKey -notmatch '^[a-z][a-z0-9-]{0,63}$') {
    throw 'ApplicationKey is invalid.'
}
if ($ModelVersion -lt 1) {
    throw 'ModelVersion must be positive.'
}

$manifest = Get-Content $SecurityManifestPath -Raw | ConvertFrom-Json
if ([int]$manifest.schemaVersion -ne 1) {
    throw 'The shared administration manifest must use schemaVersion 1.'
}
$manifestProject = ([string]$manifest.rbac.project).Trim().ToLowerInvariant()
$manifestNamespaces = @($manifest.rbac.namespaces | ForEach-Object { ([string]$_).Trim().ToLowerInvariant() } | Sort-Object -Unique)
if ([string]::IsNullOrWhiteSpace($manifestProject) -or $manifestNamespaces.Count -eq 0) {
    throw 'The shared administration manifest must declare an RBAC project and at least one namespace.'
}
foreach ($contextValue in @($manifestProject) + $manifestNamespaces) {
    if ([string]::IsNullOrWhiteSpace($contextValue) -or $contextValue.Length -gt 128 -or $contextValue.Contains(':') -or $contextValue.Contains('*')) {
        throw 'RBAC project and namespace must be concrete context segments of at most 128 characters.'
    }
}
$identityResource = @($manifest.resources | Where-Object { ([string]$_.name) -eq 'identity-access' })
if ($identityResource.Count -ne 1) {
    throw 'The shared administration manifest must contain exactly one identity-access resource.'
}

$capabilities = @()
foreach ($feature in @($identityResource[0].features)) {
    $featureName = ([string]$feature.name).Trim().ToLowerInvariant()
    foreach ($action in @($feature.actions)) {
        $actionName = ([string]$action.name).Trim().ToLowerInvariant()
        $displayName = ([string]$action.displayName).Trim()
        $capabilities += [pscustomobject]@{
            Resource = 'identity-access'
            Feature = $featureName
            Action = $actionName
            DisplayName = $displayName
        }
    }
}
if ($capabilities.Count -eq 0) {
    throw 'The shared administration manifest contains no identity-access capabilities.'
}
$capabilities = @($capabilities | Sort-Object Resource, Feature, Action)

$fingerprintBuilder = [Text.StringBuilder]::new()
function Add-FingerprintPart([Text.StringBuilder]$Builder, [string]$Name, [string]$Value) {
    [void]$Builder.Append($Name.Length).Append(':').Append($Name).Append('=').Append($Value.Length).Append(':').Append($Value).Append("`n")
}
Add-FingerprintPart $fingerprintBuilder 'schema' ([int]$manifest.schemaVersion).ToString([Globalization.CultureInfo]::InvariantCulture)
Add-FingerprintPart $fingerprintBuilder 'application' $ApplicationKey
Add-FingerprintPart $fingerprintBuilder 'model' $ModelVersion.ToString([Globalization.CultureInfo]::InvariantCulture)
Add-FingerprintPart $fingerprintBuilder 'project' $manifestProject
foreach ($namespaceValue in $manifestNamespaces) {
    Add-FingerprintPart $fingerprintBuilder 'namespace' $namespaceValue
}
foreach ($capability in $capabilities) {
    Add-FingerprintPart $fingerprintBuilder 'resource' $capability.Resource
    Add-FingerprintPart $fingerprintBuilder 'feature' $capability.Feature
    Add-FingerprintPart $fingerprintBuilder 'action' $capability.Action
    Add-FingerprintPart $fingerprintBuilder 'display' $capability.DisplayName
}
$sha256 = [Security.Cryptography.SHA256]::Create()
try {
    $manifestHashBytes = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($fingerprintBuilder.ToString()))
}
finally {
    $sha256.Dispose()
}
$manifestSha256 = ([BitConverter]::ToString($manifestHashBytes) -replace '-', '').ToLowerInvariant()

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) { $env:IDENTITY_ACCESS_POSTGRES_DATABASE } else { 'generic_identity_access_default' }
$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) { $env:IDENTITY_ACCESS_POSTGRES_USER } else { 'postgres' }

function Escape-SqlLiteral([string]$Value) {
    return $Value.Replace("'", "''")
}

$app = Escape-SqlLiteral $ApplicationKey
$rbacProjectSql = Escape-SqlLiteral $manifestProject
$namespaceValues = foreach ($namespaceValue in $manifestNamespaces) {
    $namespaceSql = Escape-SqlLiteral $namespaceValue
    "('$IdentityScopeId', '$app', $ModelVersion, '$namespaceSql')"
}
$namespaceSql = $namespaceValues -join ",`n"
$groupId = [Guid]'00000000-0000-0000-0000-000000000030'
$policyId = [Guid]'00000000-0000-0000-0000-000000000031'
$statementId = [Guid]'00000000-0000-0000-0000-000000000032'

$capabilityValues = foreach ($capability in $capabilities) {
    $resource = Escape-SqlLiteral $capability.Resource
    $feature = Escape-SqlLiteral $capability.Feature
    $action = Escape-SqlLiteral $capability.Action
    $display = Escape-SqlLiteral $capability.DisplayName
    "('$IdentityScopeId', '$app', $ModelVersion, '$resource', '$feature', '$action', '$display')"
}
$capabilitySql = $capabilityValues -join ",`n"

$sql = @"
DO `$`$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.users
        WHERE identity_scope_id = '$IdentityScopeId'
          AND user_id = '$UserId'
          AND status = 1
    ) THEN
        RAISE EXCEPTION 'Consumer application development administrator subject is not an active user';
    END IF;
END
`$`$;

-- Local-development composition only: ensure the consumer application has a model
-- row capable of hosting the shared Generic Identity administration capability set.
INSERT INTO identity_access.application_security_models
(identity_scope_id, application_key, model_version)
VALUES ('$IdentityScopeId', '$app', $ModelVersion)
ON CONFLICT DO NOTHING;

-- The namespace catalog is registration-backed. Register the derived local
-- consumer application administration security model before inserting namespaces.
INSERT INTO identity_access.application_security_model_registrations AS existing_registration
(identity_scope_id, application_key, model_version, manifest_schema_version, rbac_project, manifest_sha256)
VALUES ('$IdentityScopeId', '$app', $ModelVersion, $([int]$manifest.schemaVersion), '$rbacProjectSql', '$manifestSha256')
ON CONFLICT (identity_scope_id, application_key, model_version) DO UPDATE SET
    manifest_sha256 = CASE
        WHEN existing_registration.manifest_sha256 = EXCLUDED.manifest_sha256
        THEN existing_registration.manifest_sha256
        ELSE NULL
    END;

INSERT INTO identity_access.application_security_namespaces
(identity_scope_id, application_key, model_version, rbac_namespace)
VALUES
$namespaceSql
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.application_capabilities
(identity_scope_id, application_key, model_version,
 capability_resource, capability_feature, capability_action, display_name)
VALUES
$capabilitySql
ON CONFLICT (identity_scope_id, application_key, model_version,
             capability_resource, capability_feature, capability_action) DO UPDATE SET
    display_name = EXCLUDED.display_name;

INSERT INTO identity_access.identity_scope_administration_groups
(identity_scope_id, application_key, group_id, display_name, status)
VALUES ('$IdentityScopeId', '$app', '$groupId', 'consumer application Local Identity Administrators', 1)
ON CONFLICT (identity_scope_id, application_key, group_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    status = 1,
    row_version = identity_access.identity_scope_administration_groups.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.identity_scope_administration_group_memberships
(identity_scope_id, application_key, group_id, user_id)
VALUES ('$IdentityScopeId', '$app', '$groupId', '$UserId')
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.identity_scope_administration_policies
(identity_scope_id, application_key, policy_id, display_name, status)
VALUES ('$IdentityScopeId', '$app', '$policyId', 'consumer application Local Identity Administration', 1)
ON CONFLICT (identity_scope_id, application_key, policy_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    status = 1,
    row_version = identity_access.identity_scope_administration_policies.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.identity_scope_administration_policy_statements
(identity_scope_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES ('$IdentityScopeId', '$app', '$policyId', '$statementId', $ModelVersion,
        'identity-access', '*', '*')
ON CONFLICT (identity_scope_id, application_key, policy_id, statement_id) DO UPDATE SET
    model_version = EXCLUDED.model_version,
    capability_resource = EXCLUDED.capability_resource,
    capability_feature = EXCLUDED.capability_feature,
    capability_action = EXCLUDED.capability_action;

INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
(identity_scope_id, application_key, group_id, policy_id)
VALUES ('$IdentityScopeId', '$app', '$groupId', '$policyId')
ON CONFLICT DO NOTHING;
"@

Write-Host "Granting local Generic Identity administration authority to '$ApplicationKey' admin context..."
$sql | & psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName --single-transaction
if ($LASTEXITCODE -ne 0) {
    throw 'consumer development administrator authority bootstrap failed.'
}

$verifySql = @"
SELECT COUNT(*)
FROM identity_access.identity_scope_administration_group_memberships gm
JOIN identity_access.identity_scope_administration_group_policy_bindings b
  ON b.identity_scope_id = gm.identity_scope_id
 AND b.application_key = gm.application_key
 AND b.group_id = gm.group_id
JOIN identity_access.identity_scope_administration_policy_statements s
  ON s.identity_scope_id = b.identity_scope_id
 AND s.application_key = b.application_key
 AND s.policy_id = b.policy_id
JOIN identity_access.application_security_model_registrations r
  ON r.identity_scope_id = s.identity_scope_id
 AND r.application_key = s.application_key
 AND r.model_version = s.model_version
WHERE gm.identity_scope_id = '$IdentityScopeId'
  AND gm.application_key = '$app'
  AND gm.user_id = '$UserId'
  AND r.rbac_project = '$rbacProjectSql'
  AND s.capability_resource = 'identity-access'
  AND s.capability_feature = '*'
  AND s.capability_action = '*';
"@

$count = ($verifySql | & psql -U $postgresUser -d $databaseName -t -A).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'consumer development administrator authority verification query failed.'
}
[int]$grantCount = 0
if (-not [int]::TryParse($count, [ref]$grantCount) -or $grantCount -lt 1) {
    throw 'consumer development administrator authority verification failed.'
}

Write-Host 'Consumer application local Generic Identity administration authority: GREEN'
Write-Host "IdentityScopeId: $IdentityScopeId"
Write-Host "UserId:          $UserId"
Write-Host "ApplicationKey:  $ApplicationKey"
Write-Host "ModelVersion:    $ModelVersion"
