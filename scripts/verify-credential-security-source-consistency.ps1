$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required credential-security source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "Credential-security source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "Credential-security source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Application/Authentication/ISelfServiceCredentialService.cs",
    "src/IdentityAccess.Application/Authentication/SelfServiceCredentialService.cs",
    "src/IdentityAccess.Api/Controllers/SelfServiceCredentialsController.cs",
    "src/IdentityAccess.Api/Controllers/RecoveryPasswordResetController.cs",
    "src/IdentityAccess.Mfa.Recovery/IRecoveryPasswordResetService.cs",
    "src/IdentityAccess.Mfa.Recovery/RecoveryPasswordResetService.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0020_credential_security_hardening.sql",
    "tests/IdentityAccess.Tests/Authentication/CredentialSecurityHardeningContractTests.cs",
    "scripts/postgresql/verify-credential-security-hardening.ps1",
    "docs/ACCOUNT_RECOVERY_AND_CREDENTIAL_SECURITY.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Application/Authentication/AuthenticationOptions.cs" "SensitiveOperationMfaMaxAgeMinutes"
Require-Text "src/IdentityAccess.Application/Authentication/SelfServiceCredentialService.cs" "RecentMfaRequired"
Require-Text "src/IdentityAccess.Application/Authentication/SelfServiceCredentialService.cs" "PasswordReuseRejected"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlCredentialMutationStore.cs" "revocation_reason = 'credential_changed'"
Require-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "FOR UPDATE OF u, p, a, s"
Require-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "revocation_reason = 'account_recovery'"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0020_credential_security_hardening.sql" "'credential_changed'"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0020_credential_security_hardening.sql" "'account_recovery'"
Require-Text "src/IdentityAccess.Api/Controllers/RecoveryPasswordResetController.cs" "The supplied account-recovery proof was rejected."
Reject-Text "src/IdentityAccess.Api/Controllers/RecoveryPasswordResetRequest.cs" "AuthenticatorId"
Require-Text "clients/typescript/src/client/IdentityAccessAuthenticationClient.ts" "changePassword"
Require-Text "clients/typescript/src/client/IdentityAccessAuthenticationClient.ts" "recoverPasswordWithCode"
Reject-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "raw_recovery_code"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0020_credential_security_hardening.sql" "password_hash"
Require-Text "scripts/verify.ps1" "verify-credential-security-source-consistency.ps1"

Write-Host "Credential security source consistency validation passed."
