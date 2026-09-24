$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required WebAuthn authentication source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "WebAuthn authentication source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "WebAuthn authentication source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Mfa.WebAuthn/IWebAuthnAuthenticationService.cs",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationService.cs",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationOptions.cs",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationResponse.cs",
    "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAssertionVerifier.cs",
    "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0018_webauthn_authentication.sql",
    "tests/IdentityAccess.Tests/Mfa/WebAuthn/WebAuthnAuthenticationServiceTests.cs",
    "scripts/postgresql/verify-webauthn-authentication.ps1",
    "docs/WEBAUTHN_AUTHENTICATION.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationFactorProvider.cs" "AuthenticationFactorProviderCapabilities.Verification"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAssertionVerifier.cs" '"webauthn.get"'
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAssertionVerifier.cs" "DSASignatureFormat.Rfc3279DerSequence"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAssertionVerifier.cs" "CryptographicOperations.FixedTimeEquals"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAssertionVerifier.cs" "AttestedCredentialDataFlag"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs" "FOR UPDATE OF c, u, w"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs" "assertedSignCount <= persistedSignCount"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/PostgreSqlWebAuthnCredentialStore.cs" "backup_state = @backup_state"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0018_webauthn_authentication.sql" "challenge_hash bytea NOT NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0018_webauthn_authentication.sql" "webauthn_authentication_challenges"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0018_webauthn_authentication.sql" "private_key"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0018_webauthn_authentication.sql" "raw_challenge"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnServiceCollectionExtensions.cs" "IWebAuthnAuthenticationService"
Require-Text "tests/IdentityAccess.Tests/Mfa/WebAuthn/WebAuthnAuthenticationServiceTests.cs" "ReplayDetected"
Require-Text "scripts/verify.ps1" "verify-webauthn-authentication-source-consistency.ps1"

Write-Host "WebAuthn authentication source consistency validation passed."
