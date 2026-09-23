$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required WebAuthn registration source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "WebAuthn registration source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "WebAuthn registration source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Mfa.WebAuthn/IdentityAccess.Mfa.WebAuthn.csproj",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationFactorProvider.cs",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationService.cs",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationVerifier.cs",
    "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0017_webauthn_registration.sql",
    "tests/IdentityAccess.Tests/Mfa/WebAuthn/WebAuthnRegistrationServiceTests.cs",
    "tests/IdentityAccess.Tests/Mfa/WebAuthn/WebAuthnAuthenticationFactorProviderTests.cs",
    "scripts/postgresql/verify-webauthn-registration.ps1",
    "docs/WEBAUTHN_REGISTRATION.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationFactorProvider.cs" "AuthenticationFactorProviderCapabilities.Enrollment"
Reject-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationFactorProvider.cs" "AuthenticationFactorProviderCapabilities.Verification"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationVerifier.cs" '"webauthn.create"'
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationVerifier.cs" '"none"'
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationVerifier.cs" "CryptographicOperations.FixedTimeEquals"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationVerifier.cs" "CoseAlgorithmEs256"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs" "FOR UPDATE OF u, c"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs" "ON CONFLICT (identity_scope_id, credential_id) DO NOTHING"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0017_webauthn_registration.sql" "challenge_hash bytea NOT NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0017_webauthn_registration.sql" "cose_public_key bytea NOT NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0017_webauthn_registration.sql" "cose_algorithm smallint NOT NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0017_webauthn_registration.sql" "user_handle bytea NOT NULL"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0017_webauthn_registration.sql" "private_key"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" "webauthn_credentials"
Require-Text "src/IdentityAccess.Api/Program.cs" "AddIdentityAccessWebAuthnProvider("
Require-Text "IdentityAccess.sln" "IdentityAccess.Mfa.WebAuthn"
Require-Text "tests/IdentityAccess.Tests/IdentityAccess.Tests.csproj" "IdentityAccess.Mfa.WebAuthn.csproj"
Reject-Text "Directory.Packages.props" "System.Formats.Cbor"
Reject-Text "src/IdentityAccess.Mfa.WebAuthn/IdentityAccess.Mfa.WebAuthn.csproj" '<PackageReference Include="System.Formats.Cbor"'

Write-Host "WebAuthn registration source consistency validation passed."
