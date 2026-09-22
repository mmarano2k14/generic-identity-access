$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath

    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required OIDC source file is missing: $RelativePath"
    }

    return $path
}

function Require-Text(
    [string]$RelativePath,
    [string]$ExpectedText) {

    $path = Require-File $RelativePath
    $source = Get-Content $path -Raw

    if (-not $source.Contains($ExpectedText)) {
        throw "OIDC source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text(
    [string]$RelativePath,
    [string]$RejectedText) {

    $path = Require-File $RelativePath
    $source = Get-Content $path -Raw

    if ($source.Contains($RejectedText)) {
        throw "OIDC source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Application/Authentication/IOidcAuthorizationService.cs",
    "src/IdentityAccess.Application/Authentication/IOidcAuthorizationCodeStore.cs",
    "src/IdentityAccess.Application/Authentication/IOidcCodeService.cs",
    "src/IdentityAccess.Application/Authentication/IOidcRefreshTokenService.cs",
    "src/IdentityAccess.Application/Authentication/IOidcRefreshTokenStore.cs",
    "src/IdentityAccess.Application/Authentication/IOidcTokenIssuer.cs",
    "src/IdentityAccess.Application/Authentication/IOidcAccessTokenValidator.cs",
    "src/IdentityAccess.Application/Authentication/IOidcAccessTokenSessionValidator.cs",
    "src/IdentityAccess.Application/Authentication/OidcAccessTokenSessionValidator.cs",
    "src/IdentityAccess.Application/Authentication/OidcAuthorizationService.cs",
    "src/IdentityAccess.Infrastructure.Authentication/CryptographicOidcCodeService.cs",
    "src/IdentityAccess.Infrastructure.Authentication/CryptographicOidcRefreshTokenService.cs",
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs",
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs",
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcSigningKeyConfigurationLoader.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlOidcAuthorizationCodeStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlOidcRefreshTokenStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0012_oidc_authorization_code_pkce.sql",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0013_oidc_refresh_token_rotation.sql",
    "src/IdentityAccess.Api/Controllers/OidcAuthorizationController.cs",
    "src/IdentityAccess.Api/Controllers/OidcTokenController.cs",
    "src/IdentityAccess.Api/Controllers/OidcDiscoveryController.cs",
    "src/IdentityAccess.Api/Oidc/IOidcLocalSessionResolver.cs",
    "src/IdentityAccess.Api/Oidc/OidcLocalSessionResolver.cs",
    "src/IdentityAccess.Api/Oidc/OidcRedirectBuilder.cs",
    "src/IdentityAccess.Api/Oidc/OidcRequestParameterGuard.cs",
    "src/IdentityAccess.Api/Security/BearerAdministrationRequestContextResolver.cs",
    "src/IdentityAccess.Api/Security/CompositeAdministrationRequestContextResolver.cs"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text `
    "src/IdentityAccess.Application/Authentication/AuthenticatedSessionContext.cs" `
    "AuthenticatedAt"

Require-Text `
    "src/IdentityAccess.Application/Authentication/AuthenticationClientRegistration.cs" `
    "OidcEnabled"

Require-Text `
    "src/IdentityAccess.Application/Authentication/AuthenticationClientRegistration.cs" `
    "AllowedOidcScopes"

Require-Text `
    "src/IdentityAccess.Application/Authentication/OidcAuthorizationService.cs" `
    '"S256"'

Require-Text `
    "src/IdentityAccess.Application/Authentication/OidcAuthorizationService.cs" `
    '"openid"'

Reject-Text `
    "src/IdentityAccess.Application/Authentication/OidcAuthorizationService.cs" `
    '"plain"'

Require-Text `
    "src/IdentityAccess.Application/Authentication/IOidcAuthorizationCodeStore.cs" `
    "CreateForActiveSessionAsync"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlOidcAuthorizationCodeStore.cs" `
    "FROM identity_access.user_sessions AS s"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0012_oidc_authorization_code_pkce.sql" `
    "code_hash bytea"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0012_oidc_authorization_code_pkce.sql" `
    "trg_security_mutation_oidc_authorization_codes"

Reject-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0012_oidc_authorization_code_pkce.sql" `
    "client_secret"

Require-Text `
    "src/IdentityAccess.Api/Program.cs" `
    "builder.Environment.ContentRootPath"

Require-Text `
    "src/IdentityAccess.Api/ApiFeatureRegistration.cs" `
    "Register<IOidcAuthorizationService>"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "ActiveSigningKeyId"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "SigningKeys"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcSigningKeyConfigurationLoader.cs" `
    "SigningKeyPemPath"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcSigningKeyConfigurationLoader.cs" `
    "legacy signing-key fields cannot be combined"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "RequireService<IOidcAuthorizationCodeStore>"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs" `
    '"RS256"'

Require-Text `
    "src/IdentityAccess.Application/Authentication/IOidcTokenIssuer.cs" `
    "SigningKeys"

Require-Text `
    "src/IdentityAccess.Application/Authentication/IOidcAuthorizationService.cs" `
    "SigningKeys"

Require-Text `
    "src/IdentityAccess.Api/Controllers/OidcDiscoveryController.cs" `
    "service.SigningKeys"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs" `
    "activeRsa.SignData"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs" `
    "activeSigningKeyId"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs" `
    "private RSA key material"


Require-Text `
    "src/IdentityAccess.Application/Authentication/OidcOptions.cs" `
    "RefreshTokenLifetimeDays"

Require-Text `
    "src/IdentityAccess.Application/Authentication/IOidcAuthorizationService.cs" `
    "RefreshAsync"

Require-Text `
    "src/IdentityAccess.Application/Authentication/IOidcRefreshTokenStore.cs" `
    "RotateAsync"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/CryptographicOidcRefreshTokenService.cs" `
    "RandomNumberGenerator.GetBytes(32)"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlOidcRefreshTokenStore.cs" `
    "pg_advisory_xact_lock"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlOidcRefreshTokenStore.cs" `
    "revocation_reason = 'reuse_detected'"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlOidcRefreshTokenStore.cs" `
    "s.revoked_at IS NULL"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0013_oidc_refresh_token_rotation.sql" `
    "token_hash bytea"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0013_oidc_refresh_token_rotation.sql" `
    "CHECK (octet_length(token_hash) = 32)"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0013_oidc_refresh_token_rotation.sql" `
    "trg_security_mutation_oidc_refresh_tokens"

Reject-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0013_oidc_refresh_token_rotation.sql" `
    "raw_token"

Require-Text `
    "src/IdentityAccess.Api/Controllers/OidcTokenController.cs" `
    '"refresh_token"'

Require-Text `
    "src/IdentityAccess.Api/Controllers/OidcDiscoveryController.cs" `
    '"refresh_token"'

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "RequireService<IOidcRefreshTokenStore>"

Reject-Text `
    "src/IdentityAccess.Api/Controllers/OidcTokenController.cs" `
    "client_secret"

Reject-Text `
    "src/IdentityAccess.Api/Controllers/OidcTokenController.cs" `
    "password"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "IOidcAccessTokenValidator"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "IOidcAccessTokenSessionValidator"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" `
    "UnknownSigningKey"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" `
    "SignatureInvalid"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" `
    "IssuerMismatch"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" `
    "AudienceMismatch"

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" `
    '"identity_scope_id"'

Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" `
    '"application_key"'

Require-Text `
    "src/IdentityAccess.Application/Authentication/OidcAccessTokenSessionValidator.cs" `
    "ValidateReferenceAsync"

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlAuthenticationSessionStore.cs" `
    "ValidateReferenceAsync"

Require-Text `
    "src/IdentityAccess.Api/Security/AdministrationSecurityServiceRegistration.cs" `
    "CompositeAdministrationRequestContextResolver"

Require-Text `
    "src/IdentityAccess.Api/Security/BearerAdministrationRequestContextResolver.cs" `
    "BearerTokenInvalid"

Write-Host "OIDC source consistency validation passed."
