[CmdletBinding()]
param(
    [Guid]$IdentityScopeId = [Guid]'00000000-0000-0000-0000-000000000001',
    [Guid]$UserId = [Guid]'00000000-0000-0000-0000-000000000003',
    [Parameter(Mandatory = $true)]
    [string]$ApplicationKey,
    [Parameter(Mandatory = $true)]
    [int]$ModelVersion,
    [Parameter(Mandatory = $true)]
    [string]$SecurityManifestPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw 'psql was not found on PATH.'
}

$SecurityManifestPath = [IO.Path]::GetFullPath($SecurityManifestPath)

if (-not (Test-Path $SecurityManifestPath -PathType Leaf)) {
    throw "Application security manifest was not found: $SecurityManifestPath"
}

if ($ApplicationKey -notmatch '^[a-z][a-z0-9-]{0,63}$') {
    throw 'ApplicationKey is invalid.'
}

if ($ModelVersion -lt 1) {
    throw 'ModelVersion must be positive.'
}

$manifest = Get-Content $SecurityManifestPath -Raw | ConvertFrom-Json

if ([int]$manifest.schemaVersion -ne 1) {
    throw 'The application security manifest must use schemaVersion 1.'
}

$manifestApplicationKey = ([string]$manifest.applicationKey).Trim().ToLowerInvariant()
if ([string]::IsNullOrWhiteSpace($manifestApplicationKey)) {
    throw 'The application security manifest must declare applicationKey.'
}

if ($manifestApplicationKey -ne $ApplicationKey.ToLowerInvariant()) {
    throw "ApplicationKey '$ApplicationKey' does not match manifest applicationKey '$manifestApplicationKey'."
}

if ([int]$manifest.modelVersion -ne $ModelVersion) {
    throw "ModelVersion '$ModelVersion' does not match manifest modelVersion '$($manifest.modelVersion)'."
}

$manifestProject = ([string]$manifest.rbac.project).Trim().ToLowerInvariant()
$manifestNamespaces = @(
    $manifest.rbac.namespaces |
        ForEach-Object { ([string]$_).Trim().ToLowerInvariant() } |
        Sort-Object -Unique
)

if ([string]::IsNullOrWhiteSpace($manifestProject) -or $manifestNamespaces.Count -eq 0) {
    throw 'The application security manifest must declare an RBAC project and at least one namespace.'
}

foreach ($contextValue in @($manifestProject) + $manifestNamespaces) {
    if (
        [string]::IsNullOrWhiteSpace($contextValue) -or
        $contextValue.Length -gt 128 -or
        $contextValue.Contains(':') -or
        $contextValue.Contains('*')
    ) {
        throw 'RBAC project and namespace must be concrete context segments of at most 128 characters.'
    }
}

$resources = @($manifest.resources)
if ($resources.Count -eq 0) {
    throw 'The application security manifest must declare at least one resource.'
}

$capabilities = @()

foreach ($resource in $resources) {
    $resourceName = ([string]$resource.name).Trim().ToLowerInvariant()

    if (
        [string]::IsNullOrWhiteSpace($resourceName) -or
        $resourceName.Length -gt 128 -or
        $resourceName.Contains(':') -or
        $resourceName.Contains('*')
    ) {
        throw "Application security manifest contains an invalid resource name: '$resourceName'."
    }

    foreach ($feature in @($resource.features)) {
        $featureName = ([string]$feature.name).Trim().ToLowerInvariant()

        if (
            [string]::IsNullOrWhiteSpace($featureName) -or
            $featureName.Length -gt 128 -or
            $featureName.Contains(':') -or
            $featureName.Contains('*')
        ) {
            throw "Application security manifest contains an invalid feature name: '$featureName'."
        }

        foreach ($action in @($feature.actions)) {
            $actionName = ([string]$action.name).Trim().ToLowerInvariant()
            $displayName = ([string]$action.displayName).Trim()

            if (
                [string]::IsNullOrWhiteSpace($actionName) -or
                $actionName.Length -gt 128 -or
                $actionName.Contains(':') -or
                $actionName.Contains('*')
            ) {
                throw "Application security manifest contains an invalid action name: '$actionName'."
            }

            if ([string]::IsNullOrWhiteSpace($displayName)) {
                throw "Capability '$resourceName/$featureName/$actionName' must declare displayName."
            }

            $capabilities += [pscustomobject]@{
                Resource    = $resourceName
                Feature     = $featureName
                Action      = $actionName
                DisplayName = $displayName
            }
        }
    }
}

if ($capabilities.Count -eq 0) {
    throw 'The application security manifest contains no capabilities.'
}

$capabilities = @(
    $capabilities |
        Sort-Object Resource, Feature, Action
)

$resourceNames = @(
    $capabilities |
        Select-Object -ExpandProperty Resource -Unique |
        Sort-Object
)

$fingerprintBuilder = [Text.StringBuilder]::new()

function Add-FingerprintPart(
    [Text.StringBuilder]$Builder,
    [string]$Name,
    [string]$Value
) {
    [void]$Builder.Append($Name.Length)
    [void]$Builder.Append(':')
    [void]$Builder.Append($Name)
    [void]$Builder.Append('=')
    [void]$Builder.Append($Value.Length)
    [void]$Builder.Append(':')
    [void]$Builder.Append($Value)
    [void]$Builder.Append("`n")
}

Add-FingerprintPart $fingerprintBuilder 'schema' ([int]$manifest.schemaVersion).ToString([Globalization.CultureInfo]::InvariantCulture)
Add-FingerprintPart $fingerprintBuilder 'application' $ApplicationKey.ToLowerInvariant()
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
    $manifestHashBytes = $sha256.ComputeHash(
        [Text.Encoding]::UTF8.GetBytes($fingerprintBuilder.ToString())
    )
}
finally {
    $sha256.Dispose()
}

$manifestSha256 = (
    [BitConverter]::ToString($manifestHashBytes) -replace '-', ''
).ToLowerInvariant()

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
    $env:IDENTITY_ACCESS_POSTGRES_DATABASE
}
else {
    'generic_identity_access_default'
}

$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) {
    $env:IDENTITY_ACCESS_POSTGRES_USER
}
else {
    'postgres'
}

function Escape-SqlLiteral([string]$Value) {
    return $Value.Replace("'", "''")
}

$app = Escape-SqlLiteral $ApplicationKey.ToLowerInvariant()
$rbacProjectSql = Escape-SqlLiteral $manifestProject

$namespaceValues = foreach ($namespaceValue in $manifestNamespaces) {
    $namespaceSqlValue = Escape-SqlLiteral $namespaceValue
    "('$IdentityScopeId', '$app', $ModelVersion, '$namespaceSqlValue')"
}

$namespaceSql = $namespaceValues -join ",`n"

$groupId = [Guid]'00000000-0000-0000-0000-000000000030'
$policyId = [Guid]'00000000-0000-0000-0000-000000000031'

$capabilityValues = foreach ($capability in $capabilities) {
    $resource = Escape-SqlLiteral $capability.Resource
    $feature = Escape-SqlLiteral $capability.Feature
    $action = Escape-SqlLiteral $capability.Action
    $display = Escape-SqlLiteral $capability.DisplayName

    "('$IdentityScopeId', '$app', $ModelVersion, '$resource', '$feature', '$action', '$display')"
}

$capabilitySql = $capabilityValues -join ",`n"

$statementValues = @()
$statementIndex = 0

foreach ($resourceName in $resourceNames) {
    $statementNumber = 32 + $statementIndex
    $statementId = [Guid](
        "00000000-0000-0000-0000-{0}" -f $statementNumber.ToString('000000000000')
    )

    $resourceSqlValue = Escape-SqlLiteral $resourceName

    $statementValues += @"
('$IdentityScopeId', '$app', '$policyId', '$statementId', $ModelVersion,
 '$resourceSqlValue', '*', '*')
"@

    $statementIndex++
}

$statementSql = $statementValues -join ",`n"

$resourceVerifyValues = @(
    $resourceNames |
        ForEach-Object { "'" + (Escape-SqlLiteral $_) + "'" }
)

$resourceVerifySql = $resourceVerifyValues -join ', '

$groupDisplayName = Escape-SqlLiteral "$ApplicationKey Local Super Administrators"
$policyDisplayName = Escape-SqlLiteral "$ApplicationKey Local Super Administration"

$sql = @"
DO `$$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.users
        WHERE identity_scope_id = '$IdentityScopeId'
          AND user_id = '$UserId'
          AND status = 1
    ) THEN
        RAISE EXCEPTION 'Application development super administrator subject is not an active user';
    END IF;
END
`$$;

INSERT INTO identity_access.application_security_models
(identity_scope_id, application_key, model_version)
VALUES ('$IdentityScopeId', '$app', $ModelVersion)
ON CONFLICT DO NOTHING;

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
VALUES ('$IdentityScopeId', '$app', '$groupId', '$groupDisplayName', 1)
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
VALUES ('$IdentityScopeId', '$app', '$policyId', '$policyDisplayName', 1)
ON CONFLICT (identity_scope_id, application_key, policy_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    status = 1,
    row_version = identity_access.identity_scope_administration_policies.row_version + 1,
    updated_at = transaction_timestamp();

-- Replace only the local development super-admin policy statements for this
-- application. This removes stale grants from earlier local bootstrap attempts.
DELETE FROM identity_access.identity_scope_administration_policy_statements
WHERE identity_scope_id = '$IdentityScopeId'
  AND application_key = '$app'
  AND policy_id = '$policyId';

INSERT INTO identity_access.identity_scope_administration_policy_statements
(identity_scope_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES
$statementSql;

INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
(identity_scope_id, application_key, group_id, policy_id)
VALUES ('$IdentityScopeId', '$app', '$groupId', '$policyId')
ON CONFLICT DO NOTHING;
"@

Write-Host "Granting local application super-admin authority to '$ApplicationKey'..."
Write-Host "Security model: $ApplicationKey / v$ModelVersion"
Write-Host "RBAC project:   $manifestProject"
Write-Host "Resources:      $($resourceNames -join ', ')"

$sql | & psql `
    -U $postgresUser `
    -v ON_ERROR_STOP=1 `
    -d $databaseName `
    --single-transaction

if ($LASTEXITCODE -ne 0) {
    throw 'Application development super administrator authority bootstrap failed.'
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
  AND s.model_version = $ModelVersion
  AND s.capability_resource IN ($resourceVerifySql)
  AND s.capability_feature = '*'
  AND s.capability_action = '*';
"@

$count = (
    $verifySql |
        & psql -U $postgresUser -d $databaseName -t -A
).Trim()

if ($LASTEXITCODE -ne 0) {
    throw 'Application development super administrator authority verification query failed.'
}

[int]$grantCount = 0

if (
    -not [int]::TryParse($count, [ref]$grantCount) -or
    $grantCount -lt $resourceNames.Count
) {
    throw "Application development super administrator authority verification failed. Expected at least $($resourceNames.Count) wildcard statement(s), found '$count'."
}

Write-Host 'Application local super-admin authority: GREEN'
Write-Host "IdentityScopeId: $IdentityScopeId"
Write-Host "UserId:          $UserId"
Write-Host "ApplicationKey:  $ApplicationKey"
Write-Host "ModelVersion:    $ModelVersion"
Write-Host "RBAC project:    $manifestProject"
Write-Host "Namespaces:      $($manifestNamespaces -join ', ')"
Write-Host "Resources:       $($resourceNames -join ', ')"
Write-Host "Capabilities:    $($capabilities.Count)"
