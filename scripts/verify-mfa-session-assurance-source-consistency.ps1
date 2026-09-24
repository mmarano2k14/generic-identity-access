$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required MFA session-assurance source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "MFA session-assurance source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "MFA session-assurance source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Application/Authentication/AuthenticationAssurance.cs",
    "src/IdentityAccess.Application/Authentication/AuthenticationAssuranceLevel.cs",
    "src/IdentityAccess.Application/Authentication/AuthenticationMethodReferences.cs",
    "src/IdentityAccess.Application/Authentication/IAuthenticationAssuranceService.cs",
    "src/IdentityAccess.Application/Authentication/AuthenticationAssuranceService.cs",
    "src/IdentityAccess.Api/Controllers/MfaSessionController.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0019_session_authentication_assurance.sql",
    "tests/IdentityAccess.Tests/Authentication/AuthenticationAssuranceTests.cs",
    "tests/IdentityAccess.Tests/PostgreSql/PostgreSqlAuthenticationAssuranceSchemaTests.cs",
    "scripts/postgresql/verify-session-assurance.ps1",
    "docs/MFA_SESSION_ASSURANCE_OIDC.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlAuthenticationSessionStore.cs" "UpgradeAssuranceAsync"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlAuthenticationSessionStore.cs" "FOR UPDATE OF s"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlAuthenticationSessionStore.cs" "assurance_verified_at"
Require-Text "src/IdentityAccess.Application/Authentication/OidcAuthorizationService.cs" "MfaRequired"
Require-Text "src/IdentityAccess.Application/Authentication/OidcAuthorizationService.cs" "MfaMaxAgeMinutes"
Require-Text "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs" '["acr"]'
Require-Text "src/IdentityAccess.Infrastructure.Authentication/RsaOidcTokenIssuer.cs" '["amr"]'
Require-Text "src/IdentityAccess.Infrastructure.Authentication/RsaOidcAccessTokenValidator.cs" '"auth_time"'
Require-Text "src/IdentityAccess.Api/Oidc/OidcRedirectBuilder.cs" '"interaction_required"'
Require-Text "clients/typescript/src/client/IdentityAccessAuthenticationClient.ts" "verifyTotp"
Require-Text "clients/typescript/src/client/IdentityAccessAuthenticationClient.ts" "completeWebAuthnStepUp"
Require-Text "clients/typescript/src/client/IdentityAccessProtocolCodec.ts" '"interaction_required"'
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0019_session_authentication_assurance.sql" "ARRAY['pwd']::text[]"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0019_session_authentication_assurance.sql" "session_token"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0019_session_authentication_assurance.sql" "private_key"
Require-Text "scripts/verify.ps1" "verify-mfa-session-assurance-source-consistency.ps1"

Write-Host "MFA session assurance source consistency validation passed."
