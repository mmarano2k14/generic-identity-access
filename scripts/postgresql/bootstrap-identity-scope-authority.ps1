param(
    [Parameter(Mandatory = $true)]
    [Guid]$IdentityScopeId,

    [Parameter(Mandatory = $true)]
    [Guid]$UserId,

    [Parameter(Mandatory = $true)]
    [string]$ApplicationKey,

    [Parameter(Mandatory = $true)]
    [int]$ModelVersion,

    [string]$CapabilityResource = "identity-access",
    [string]$CapabilityFeature = "*",
    [string]$CapabilityAction = "*",

    [Guid]$GroupId = [Guid]::Empty,
    [Guid]$PolicyId = [Guid]::Empty,
    [Guid]$StatementId = [Guid]::Empty
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

if ($GroupId -eq [Guid]::Empty) {
    $GroupId = [Guid]::NewGuid()
}

if ($PolicyId -eq [Guid]::Empty) {
    $PolicyId = [Guid]::NewGuid()
}

if ($StatementId -eq [Guid]::Empty) {
    $StatementId = [Guid]::NewGuid()
}

if ($ApplicationKey -notmatch '^[a-z][a-z0-9-]{0,63}$') {
    throw "ApplicationKey is invalid."
}

foreach ($segment in @($CapabilityResource, $CapabilityFeature, $CapabilityAction)) {
    if ($segment -ne "*" -and $segment -notmatch '^[a-z][a-z0-9-]{0,63}$') {
        throw "Capability pattern segments must be '*' or canonical lowercase slugs."
    }
}

if ($CapabilityResource -eq "*" -and $CapabilityFeature -ne "*") {
    throw "Unsupported capability wildcard shape."
}

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
    $env:IDENTITY_ACCESS_POSTGRES_DATABASE
} else {
    "generic_identity_access_default"
}

$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) {
    $env:IDENTITY_ACCESS_POSTGRES_USER
} else {
    "postgres"
}

function Escape-SqlLiteral([string]$Value) {
    return $Value.Replace("'", "''")
}

$app = Escape-SqlLiteral $ApplicationKey
$resource = Escape-SqlLiteral $CapabilityResource
$feature = Escape-SqlLiteral $CapabilityFeature
$action = Escape-SqlLiteral $CapabilityAction

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
        RAISE EXCEPTION 'bootstrap subject is not an active user in the identity scope';
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM identity_access.application_security_models
        WHERE identity_scope_id = '$IdentityScopeId'
          AND application_key = '$app'
          AND model_version = $ModelVersion
    ) THEN
        RAISE EXCEPTION 'requested application security model does not exist';
    END IF;
END
`$`$;

INSERT INTO identity_access.identity_scope_administration_groups
(identity_scope_id, application_key, group_id, display_name, status)
VALUES
('$IdentityScopeId', '$app', '$GroupId', 'Identity Scope Administrators', 1);

INSERT INTO identity_access.identity_scope_administration_group_memberships
(identity_scope_id, application_key, group_id, user_id)
VALUES
('$IdentityScopeId', '$app', '$GroupId', '$UserId');

INSERT INTO identity_access.identity_scope_administration_policies
(identity_scope_id, application_key, policy_id, display_name, status)
VALUES
('$IdentityScopeId', '$app', '$PolicyId', 'Identity Scope Administration Bootstrap', 1);

INSERT INTO identity_access.identity_scope_administration_policy_statements
(identity_scope_id, application_key, policy_id, statement_id, model_version,
 capability_resource, capability_feature, capability_action)
VALUES
('$IdentityScopeId', '$app', '$PolicyId', '$StatementId', $ModelVersion,
 '$resource', '$feature', '$action');

INSERT INTO identity_access.identity_scope_administration_group_policy_bindings
(identity_scope_id, application_key, group_id, policy_id)
VALUES
('$IdentityScopeId', '$app', '$GroupId', '$PolicyId');
"@

Write-Host "Creating explicit identity-scope administration bootstrap assignment..."

& psql `
    -U $postgresUser `
    -v ON_ERROR_STOP=1 `
    -d $databaseName `
    --single-transaction `
    -c $sql

if ($LASTEXITCODE -ne 0) {
    throw "Identity-scope authority bootstrap failed."
}

Write-Host "Identity-scope authority bootstrap created."
Write-Host "GroupId:     $GroupId"
Write-Host "PolicyId:    $PolicyId"
Write-Host "StatementId: $StatementId"
Write-Host "Retain these identifiers for explicit lifecycle management."
