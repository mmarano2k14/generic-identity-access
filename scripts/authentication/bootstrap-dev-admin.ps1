[CmdletBinding()]
param(
    [Guid]$IdentityScopeId = [Guid]'00000000-0000-0000-0000-000000000001',
    [Guid]$TenantId = [Guid]'00000000-0000-0000-0000-000000000002',
    [Guid]$UserId = [Guid]'00000000-0000-0000-0000-000000000003',
    [Guid]$MembershipId = [Guid]'00000000-0000-0000-0000-000000000004',
    [string]$ApplicationKey = 'admin-web',
    [int]$ModelVersion = 1,
    [string]$SecurityManifestPath,
    [string]$RbacProject = 'identity-access',
    [string]$RbacNamespace = 'administration',
    [string]$LoginIdentifier = 'admin',
    [string]$DisplayName = 'Local Administrator',
    [SecureString]$Password,
    [switch]$WriteNextJsEnvironment = $true
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw 'psql was not found on PATH.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found on PATH.'
}
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($SecurityManifestPath)) {
    $SecurityManifestPath = Join-Path $root 'config\identity-access-admin-security-manifest.json'
}
$SecurityManifestPath = [IO.Path]::GetFullPath($SecurityManifestPath)
if (-not (Test-Path $SecurityManifestPath -PathType Leaf)) {
    throw "Application security manifest was not found: $SecurityManifestPath"
}

$manifest = Get-Content $SecurityManifestPath -Raw | ConvertFrom-Json -Depth 32
if ($manifest.schemaVersion -ne 1) {
    throw 'The development bootstrap requires application security manifest schemaVersion 1.'
}
if ($manifest.applicationKey -ne $ApplicationKey) {
    throw "ApplicationKey must match the application security manifest: $($manifest.applicationKey)"
}
if ([int]$manifest.modelVersion -ne $ModelVersion) {
    throw "ModelVersion must match the application security manifest: $($manifest.modelVersion)"
}
if ($ApplicationKey -notmatch '^[a-z][a-z0-9-]{0,63}$') {
    throw 'ApplicationKey is invalid.'
}
if ($ModelVersion -lt 1) {
    throw 'ModelVersion must be positive.'
}

$manifestProject = ([string]$manifest.rbac.project).Trim().ToLowerInvariant()
$manifestNamespaces = @($manifest.rbac.namespaces | ForEach-Object { ([string]$_).Trim().ToLowerInvariant() } | Sort-Object -Unique)
if ($manifestNamespaces.Count -eq 0) {
    throw 'The application security manifest must declare at least one RBAC namespace.'
}
foreach ($contextValue in @($manifestProject) + $manifestNamespaces) {
    if ([string]::IsNullOrWhiteSpace($contextValue) -or $contextValue.Length -gt 128 -or $contextValue.Contains(':') -or $contextValue.Contains('*')) {
        throw 'RBAC project and namespace must be concrete context segments of at most 128 characters.'
    }
}

$RbacProject = $RbacProject.Trim().ToLowerInvariant()
$RbacNamespace = $RbacNamespace.Trim().ToLowerInvariant()
if ($manifestProject -ne $RbacProject) {
    throw "RbacProject must match the application security manifest: $manifestProject"
}
if ($RbacNamespace -notin $manifestNamespaces) {
    throw "RbacNamespace must be one of the namespaces declared by the application security manifest."
}
if ([string]::IsNullOrWhiteSpace($LoginIdentifier)) {
    throw 'LoginIdentifier is required.'
}
if ([string]::IsNullOrWhiteSpace($DisplayName)) {
    throw 'DisplayName is required.'
}

if ($null -eq $Password) {
    $Password = Read-Host 'Password for local admin (12-256 characters)' -AsSecureString
}

$hasherProject = Join-Path $root 'tools\IdentityAccess.DevPasswordHasher\IdentityAccess.DevPasswordHasher.csproj'
$hasherDll = Join-Path $root 'tools\IdentityAccess.DevPasswordHasher\bin\Release\net10.0\IdentityAccess.DevPasswordHasher.dll'

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) { $env:IDENTITY_ACCESS_POSTGRES_DATABASE } else { 'generic_identity_access_default' }
$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) { $env:IDENTITY_ACCESS_POSTGRES_USER } else { 'postgres' }

function Escape-SqlLiteral([string]$Value) {
    return $Value.Replace("'", "''")
}

$plainPassword = [System.Net.NetworkCredential]::new('', $Password).Password
try {
    if ($plainPassword.Length -lt 12 -or $plainPassword.Length -gt 256) {
        throw 'Password must contain 12 to 256 characters.'
    }

    & dotnet build $hasherProject -c Release --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw 'Development password hasher build failed.'
    }

    $hashOutput = $plainPassword | & dotnet $hasherDll $IdentityScopeId $UserId
    if ($LASTEXITCODE -ne 0) {
        throw 'Development password hashing failed.'
    }

    $passwordHash = ($hashOutput | Select-Object -Last 1).Trim()
    if ([string]::IsNullOrWhiteSpace($passwordHash)) {
        throw 'Development password hasher returned an empty hash.'
    }

    $normalizedLogin = $LoginIdentifier.Trim().Normalize([Text.NormalizationForm]::FormKC).ToUpperInvariant()

    $app = Escape-SqlLiteral $ApplicationKey
    $login = Escape-SqlLiteral $LoginIdentifier.Trim()
    $normalized = Escape-SqlLiteral $normalizedLogin
    $display = Escape-SqlLiteral $DisplayName.Trim()
    $hash = Escape-SqlLiteral $passwordHash

    $scopeAdminGroupId = [Guid]'00000000-0000-0000-0000-000000000010'
    $scopeAdminPolicyId = [Guid]'00000000-0000-0000-0000-000000000011'
    $scopeAdminStatementId = [Guid]'00000000-0000-0000-0000-000000000012'
    $tenantAdminGroupId = [Guid]'00000000-0000-0000-0000-000000000020'
    $tenantAdminPolicyId = [Guid]'00000000-0000-0000-0000-000000000021'

    $manifestCapabilities = @()
    foreach ($resource in @($manifest.resources)) {
        $resourceName = ([string]$resource.name).Trim().ToLowerInvariant()
        if ($resourceName -notmatch '^[a-z][a-z0-9-]{0,63}$') {
            throw "Invalid manifest resource: $resourceName"
        }
        foreach ($feature in @($resource.features)) {
            $featureName = ([string]$feature.name).Trim().ToLowerInvariant()
            if ($featureName -notmatch '^[a-z][a-z0-9-]{0,63}$') {
                throw "Invalid manifest feature: $featureName"
            }
            foreach ($action in @($feature.actions)) {
                $actionName = ([string]$action.name).Trim().ToLowerInvariant()
                $actionDisplayName = ([string]$action.displayName).Trim()
                if ($actionName -notmatch '^[a-z][a-z0-9-]{0,63}$') {
                    throw "Invalid manifest action: $actionName"
                }
                if ([string]::IsNullOrWhiteSpace($actionDisplayName) -or $actionDisplayName.Length -gt 256) {
                    throw "Manifest action displayName must contain between 1 and 256 characters."
                }
                $manifestCapabilities += [pscustomobject]@{
                    Resource = $resourceName
                    Feature = $featureName
                    Action = $actionName
                    DisplayName = $actionDisplayName
                }
            }
        }
    }

    $manifestCapabilities = @($manifestCapabilities | Sort-Object Resource, Feature, Action)
    if ($manifestCapabilities.Count -eq 0) {
        throw 'The application security manifest must declare at least one capability.'
    }
    $duplicateCapabilities = $manifestCapabilities |
        Group-Object { "$($_.Resource)|$($_.Feature)|$($_.Action)" } |
        Where-Object Count -gt 1
    if ($duplicateCapabilities) {
        throw 'The application security manifest contains duplicate concrete capabilities.'
    }

    $capabilityValues = foreach ($capability in $manifestCapabilities) {
        $resourceSql = Escape-SqlLiteral $capability.Resource
        $featureSql = Escape-SqlLiteral $capability.Feature
        $actionSql = Escape-SqlLiteral $capability.Action
        $displayNameSql = Escape-SqlLiteral $capability.DisplayName
        "('$IdentityScopeId', '$app', $ModelVersion, '$resourceSql', '$featureSql', '$actionSql', '$displayNameSql')"
    }
    $capabilitySql = $capabilityValues -join ",`n"

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
    foreach ($capability in $manifestCapabilities) {
        Add-FingerprintPart $fingerprintBuilder 'resource' $capability.Resource
        Add-FingerprintPart $fingerprintBuilder 'feature' $capability.Feature
        Add-FingerprintPart $fingerprintBuilder 'action' $capability.Action
        Add-FingerprintPart $fingerprintBuilder 'display' $capability.DisplayName
    }
    $manifestHashBytes = [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($fingerprintBuilder.ToString()))
    $manifestSha256 = [Convert]::ToHexString($manifestHashBytes).ToLowerInvariant()
    $rbacProjectSql = Escape-SqlLiteral $manifestProject
    $namespaceValues = foreach ($namespaceValue in $manifestNamespaces) {
        $namespaceSql = Escape-SqlLiteral $namespaceValue
        "('$IdentityScopeId', '$app', $ModelVersion, '$namespaceSql')"
    }
    $namespaceSql = $namespaceValues -join ",`n"

    $sql = @"
INSERT INTO identity_access.users
(identity_scope_id, user_id, display_name, status)
VALUES ('$IdentityScopeId', '$UserId', '$display', 1)
ON CONFLICT (identity_scope_id, user_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    status = 1,
    row_version = identity_access.users.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.tenants
(identity_scope_id, tenant_id, display_name, status)
VALUES ('$IdentityScopeId', '$TenantId', 'Local Administration Tenant', 1)
ON CONFLICT (identity_scope_id, tenant_id) DO UPDATE SET
    status = 1,
    row_version = identity_access.tenants.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.tenant_memberships
(identity_scope_id, membership_id, tenant_id, user_id, status)
VALUES ('$IdentityScopeId', '$MembershipId', '$TenantId', '$UserId', 1)
ON CONFLICT (identity_scope_id, tenant_id, user_id) DO UPDATE SET
    status = 1,
    row_version = identity_access.tenant_memberships.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.application_security_models
(identity_scope_id, application_key, model_version)
VALUES ('$IdentityScopeId', '$app', $ModelVersion)
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.application_security_model_registrations AS existing_registration
(identity_scope_id, application_key, model_version, manifest_schema_version, rbac_project, manifest_sha256)
VALUES ('$IdentityScopeId', '$app', $ModelVersion, 1, '$rbacProjectSql', '$manifestSha256')
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
(identity_scope_id, application_key, model_version, capability_resource, capability_feature, capability_action, display_name)
VALUES
$capabilitySql
ON CONFLICT (identity_scope_id, application_key, model_version,
             capability_resource, capability_feature, capability_action) DO UPDATE SET
    display_name = EXCLUDED.display_name;

INSERT INTO identity_access.password_credentials
(identity_scope_id, user_id, login_identifier, normalized_login_identifier, password_hash,
 failed_access_count, lockout_until)
VALUES
('$IdentityScopeId', '$UserId', '$login', '$normalized', '$hash', 0, NULL)
ON CONFLICT (identity_scope_id, user_id) DO UPDATE SET
    login_identifier = EXCLUDED.login_identifier,
    normalized_login_identifier = EXCLUDED.normalized_login_identifier,
    password_hash = EXCLUDED.password_hash,
    failed_access_count = 0,
    lockout_until = NULL,
    row_version = identity_access.password_credentials.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.identity_scope_administration_groups
(identity_scope_id, application_key, group_id, display_name, status)
VALUES ('$IdentityScopeId', '$app', '$scopeAdminGroupId', 'Local Identity Scope Administrators', 1)
ON CONFLICT (identity_scope_id, application_key, group_id) DO UPDATE SET
    status = 1,
    row_version = identity_access.identity_scope_administration_groups.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.identity_scope_administration_group_memberships
(identity_scope_id, application_key, group_id, user_id)
VALUES ('$IdentityScopeId', '$app', '$scopeAdminGroupId', '$UserId')
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.identity_scope_administration_policies
(identity_scope_id, application_key, policy_id, display_name, status)
VALUES ('$IdentityScopeId', '$app', '$scopeAdminPolicyId', 'Local Identity Scope Administration', 1)
ON CONFLICT (identity_scope_id, application_key, policy_id) DO UPDATE SET
    status = 1,
    row_version = identity_access.identity_scope_administration_policies.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.identity_scope_administration_policy_statements
(identity_scope_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES ('$IdentityScopeId', '$app', '$scopeAdminPolicyId', '$scopeAdminStatementId', $ModelVersion,
        'identity-access', '*', '*')
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
(identity_scope_id, application_key, group_id, policy_id)
VALUES ('$IdentityScopeId', '$app', '$scopeAdminGroupId', '$scopeAdminPolicyId')
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.user_groups
(identity_scope_id, tenant_id, application_key, group_id, display_name, status)
VALUES ('$IdentityScopeId', '$TenantId', '$app', '$tenantAdminGroupId', 'Local Tenant Administrators', 1)
ON CONFLICT (identity_scope_id, tenant_id, application_key, group_id) DO UPDATE SET
    status = 1,
    row_version = identity_access.user_groups.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.group_memberships
(identity_scope_id, tenant_id, application_key, group_id, tenant_membership_id)
VALUES ('$IdentityScopeId', '$TenantId', '$app', '$tenantAdminGroupId', '$MembershipId')
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.managed_policies
(identity_scope_id, application_key, policy_id, policy_key, display_name, status, default_version)
VALUES ('$IdentityScopeId', '$app', '$tenantAdminPolicyId', 'local-tenant-administration', 'Local Tenant Administration', 1, NULL)
ON CONFLICT (identity_scope_id, application_key, policy_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    status = 1,
    row_version = identity_access.managed_policies.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.managed_policy_versions
(identity_scope_id, application_key, policy_id, policy_version, model_version)
VALUES ('$IdentityScopeId', '$app', '$tenantAdminPolicyId', $ModelVersion, $ModelVersion)
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.managed_policy_statements
(identity_scope_id, application_key, policy_id, policy_version, model_version, statement_id,
 capability_resource, capability_feature, capability_action)
SELECT '$IdentityScopeId', '$app', '$tenantAdminPolicyId', $ModelVersion, $ModelVersion, gen_random_uuid(),
       c.capability_resource, c.capability_feature, c.capability_action
FROM identity_access.application_capabilities AS c
WHERE c.identity_scope_id = '$IdentityScopeId'
  AND c.application_key = '$app'
  AND c.model_version = $ModelVersion
  AND NOT EXISTS
  (
      SELECT 1
      FROM identity_access.managed_policy_statements AS existing
      WHERE existing.identity_scope_id = '$IdentityScopeId'
        AND existing.application_key = '$app'
        AND existing.policy_id = '$tenantAdminPolicyId'
        AND existing.policy_version = $ModelVersion
        AND existing.capability_resource = c.capability_resource
        AND existing.capability_feature = c.capability_feature
        AND existing.capability_action = c.capability_action
  );

UPDATE identity_access.managed_policy_versions
SET published_at = transaction_timestamp()
WHERE identity_scope_id = '$IdentityScopeId'
  AND application_key = '$app'
  AND policy_id = '$tenantAdminPolicyId'
  AND policy_version = $ModelVersion
  AND published_at IS NULL;

UPDATE identity_access.managed_policies
SET default_version = $ModelVersion,
    row_version = row_version + 1,
    updated_at = transaction_timestamp()
WHERE identity_scope_id = '$IdentityScopeId'
  AND application_key = '$app'
  AND policy_id = '$tenantAdminPolicyId';

DELETE FROM identity_access.managed_group_policy_bindings
WHERE identity_scope_id = '$IdentityScopeId'
  AND tenant_id = '$TenantId'
  AND application_key = '$app'
  AND group_id = '$tenantAdminGroupId'
  AND policy_id = '$tenantAdminPolicyId';

INSERT INTO identity_access.managed_group_policy_bindings
(identity_scope_id, tenant_id, application_key, group_id, policy_id, policy_version,
 resource_scope_id, include_descendants)
VALUES ('$IdentityScopeId', '$TenantId', '$app', '$tenantAdminGroupId', '$tenantAdminPolicyId', $ModelVersion,
        NULL, FALSE);
"@

    Write-Host 'Creating/updating local development administrator...'
    $sql | & psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName --single-transaction
    if ($LASTEXITCODE -ne 0) {
        throw 'Development administrator bootstrap failed.'
    }

    if ($WriteNextJsEnvironment) {
        $nextEnvironmentPath = Join-Path $root 'examples\nextjs\admin\.env.local'
        $nextEnvironment = @"
IDENTITY_ACCESS_API_BASE_URL=http://127.0.0.1:5080
IDENTITY_ACCESS_IDENTITY_SCOPE_ID=$IdentityScopeId
IDENTITY_ACCESS_APPLICATION_KEY=$ApplicationKey
IDENTITY_ACCESS_OIDC_CLIENT_ID=$ApplicationKey
IDENTITY_ACCESS_OIDC_REDIRECT_URI=http://127.0.0.1:3000/auth/callback
IDENTITY_ACCESS_BEARER_COOKIE_NAME=identity_access_bearer
"@
        [IO.File]::WriteAllText($nextEnvironmentPath, $nextEnvironment, [Text.UTF8Encoding]::new($false))
        Write-Host "Next.js runtime environment written: $nextEnvironmentPath"
    }

    Write-Host ''
    Write-Host 'Local development administrator is ready.'
    Write-Host "Login:           $LoginIdentifier"
    Write-Host "UserId:          $UserId"
    Write-Host "IdentityScopeId: $IdentityScopeId"
    Write-Host "TenantId:        $TenantId"
    Write-Host "ApplicationKey:  $ApplicationKey"
    Write-Host 'Password:        the secure password entered for this bootstrap run'
}
finally {
    $plainPassword = $null
}
