[CmdletBinding()]
param(
    [Guid]$IdentityScopeId = [Guid]'00000000-0000-0000-0000-000000000001',
    [Guid]$TenantId = [Guid]'00000000-0000-0000-0000-000000000002',
    [Guid]$UserId = [Guid]'00000000-0000-0000-0000-000000000003',
    [Guid]$MembershipId = [Guid]'00000000-0000-0000-0000-000000000004',
    [string]$ApplicationKey = 'admin-web',
    [int]$ModelVersion = 1,
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
if ($ApplicationKey -notmatch '^[a-z][a-z0-9-]{0,63}$') {
    throw 'ApplicationKey is invalid.'
}
if ($ModelVersion -lt 1) {
    throw 'ModelVersion must be positive.'
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

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
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
    $tenantAdminStatementId = [Guid]'00000000-0000-0000-0000-000000000022'

    $features = @(
        'user', 'tenant', 'tenant-membership', 'group', 'group-membership', 'credential',
        'policy', 'policy-statement', 'policy-binding', 'scope-type', 'resource-scope', 'session', 'mfa-policy', 'mfa-authenticator',
        'scope-authority-group', 'scope-authority-membership', 'scope-authority-policy',
        'scope-authority-statement', 'scope-authority-binding'
    )

    $capabilityValues = foreach ($feature in $features) {
        $escapedFeature = Escape-SqlLiteral $feature
        foreach ($action in @('read', 'write')) {
            "('$IdentityScopeId', '$app', $ModelVersion, 'identity-access', '$escapedFeature', '$action', 'Identity Access $escapedFeature $action')"
        }
    }

    $capabilitySql = $capabilityValues -join ",`n"

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

INSERT INTO identity_access.application_capabilities
(identity_scope_id, application_key, model_version, capability_resource, capability_feature, capability_action, display_name)
VALUES
$capabilitySql
ON CONFLICT DO NOTHING;

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

INSERT INTO identity_access.permission_policies
(identity_scope_id, tenant_id, application_key, policy_id, display_name, status)
VALUES ('$IdentityScopeId', '$TenantId', '$app', '$tenantAdminPolicyId', 'Local Tenant Administration', 1)
ON CONFLICT (identity_scope_id, tenant_id, application_key, policy_id) DO UPDATE SET
    status = 1,
    row_version = identity_access.permission_policies.row_version + 1,
    updated_at = transaction_timestamp();

INSERT INTO identity_access.policy_statements
(identity_scope_id, tenant_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES ('$IdentityScopeId', '$TenantId', '$app', '$tenantAdminPolicyId', '$tenantAdminStatementId', $ModelVersion,
        'identity-access', '*', '*')
ON CONFLICT DO NOTHING;

INSERT INTO identity_access.group_policy_bindings
(identity_scope_id, tenant_id, application_key, group_id, policy_id)
VALUES ('$IdentityScopeId', '$TenantId', '$app', '$tenantAdminGroupId', '$tenantAdminPolicyId')
ON CONFLICT DO NOTHING;
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
IDENTITY_ACCESS_TENANT_ID=$TenantId
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
