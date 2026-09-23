[CmdletBinding()]
param(
    [string]$RbacReferenceDirectory = $env:IDENTITY_ACCESS_RBAC_REFERENCE_DIRECTORY,
    [string]$RoutingFile = '..\..\config\routing.admin-local.example.json',
    [string]$SigningKeyPath = '..\..\secrets\oidc-dev-signing-key.pem'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($env:IDENTITY_ACCESS_POSTGRES_DEFAULT)) {
    throw 'IDENTITY_ACCESS_POSTGRES_DEFAULT must contain the local PostgreSQL connection string.'
}
if ([string]::IsNullOrWhiteSpace($RbacReferenceDirectory)) {
    throw 'Provide -RbacReferenceDirectory or set IDENTITY_ACCESS_RBAC_REFERENCE_DIRECTORY.'
}
if (-not (Test-Path -LiteralPath $RbacReferenceDirectory)) {
    throw 'The external RBAC reference directory does not exist.'
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$apiProject = Join-Path $root 'src\IdentityAccess.Api\IdentityAccess.Api.csproj'
$signingKeyAbsolute = [IO.Path]::GetFullPath((Join-Path (Join-Path $root 'src\IdentityAccess.Api') $SigningKeyPath))

if (-not (Test-Path -LiteralPath $signingKeyAbsolute)) {
    & (Join-Path $PSScriptRoot 'create-dev-oidc-signing-key.ps1') -OutputPath $signingKeyAbsolute
}

$values = [ordered]@{
    'IdentityAccess__Routing__Provider' = 'configuration'
    'IdentityAccess__Routing__FilePath' = $RoutingFile
    'IdentityAccess__PostgreSql__Enabled' = 'true'
    'IdentityAccess__Authentication__Enabled' = 'true'
    'IdentityAccess__Authentication__SessionLifetimeMinutes' = '60'
    'IdentityAccess__Authentication__LockoutAttempts' = '5'
    'IdentityAccess__Authentication__LockoutMinutes' = '15'
    'IdentityAccess__Authentication__Clients__0__ClientId' = 'admin-web'
    'IdentityAccess__Authentication__Clients__0__ApplicationKey' = 'admin-web'
    'IdentityAccess__Authentication__Clients__0__AuthenticationContextKey' = 'admin-web-primary'
    'IdentityAccess__Authentication__Clients__0__RedirectUris__0' = 'http://127.0.0.1:3000/auth/callback'
    'IdentityAccess__Authentication__Clients__0__PostLogoutRedirectUris__0' = 'http://127.0.0.1:3000/'
    'IdentityAccess__Authentication__Clients__0__OidcEnabled' = 'true'
    'IdentityAccess__Authentication__Clients__0__AllowedOidcScopes__0' = 'openid'
    'IdentityAccess__Authentication__Oidc__Enabled' = 'true'
    'IdentityAccess__Authentication__Oidc__Issuer' = 'http://127.0.0.1:5080'
    'IdentityAccess__Authentication__Oidc__AccessTokenAudience' = 'identity-access-api'
    'IdentityAccess__Authentication__Oidc__ActiveSigningKeyId' = 'admin-local-key'
    'IdentityAccess__Authentication__Oidc__SigningKeys__0__KeyId' = 'admin-local-key'
    'IdentityAccess__Authentication__Oidc__SigningKeys__0__PemPath' = $SigningKeyPath
    'IdentityAccess__Authorization__Enabled' = 'true'
    'IdentityAccess__Authorization__Provider' = 'multiplexed'
    'IdentityAccess__Authorization__RbacProject' = 'identity-access'
    'IdentityAccess__Authorization__RbacNamespace' = 'administration'
    'IdentityAccess__Authorization__ReferenceDirectory' = [IO.Path]::GetFullPath($RbacReferenceDirectory)
}

$previous = @{}
foreach ($entry in $values.GetEnumerator()) {
    $previous[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key, 'Process')
    [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, 'Process')
}

try {
    Write-Host 'Starting Generic Identity Access API for the local administration host...'
    Write-Host 'API:     http://127.0.0.1:5080'
    Write-Host 'Swagger: http://127.0.0.1:5080/swagger'
    & dotnet run --project $apiProject --launch-profile http
    if ($LASTEXITCODE -ne 0) {
        throw 'Development administration API exited with a non-zero status.'
    }
}
finally {
    foreach ($entry in $previous.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, 'Process')
    }
}
