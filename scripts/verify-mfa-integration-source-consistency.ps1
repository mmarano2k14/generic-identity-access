$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required MFA integration source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "MFA integration source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Application/Authentication/Mfa/IMfaProviderPolicyGuard.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/MfaProviderPolicyGuard.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/MfaProviderPolicyDecision.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/MfaProviderPolicyException.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/MfaUserSecurityState.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/UserAuthenticatorRevocationDecision.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/UserAuthenticatorRevocationResult.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/MfaPolicyComplianceException.cs",
    "tests/IdentityAccess.Tests/Mfa/MfaProviderPolicyGuardTests.cs",
    "tests/IdentityAccess.Tests/Mfa/MfaUserSecurityStateTests.cs",
    "tests/IdentityAccess.Tests/PostgreSql/PostgreSqlMfaIntegrationHardeningContractTests.cs",
    "docs/MFA_INTEGRATION_HARDENING.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" "IMfaProviderPolicyGuard"
Require-Text "src/IdentityAccess.Mfa.Totp/TotpAuthenticationFactorService.cs" "EnsurePolicyAllowedAsync"
Require-Text "src/IdentityAccess.Mfa.Recovery/RecoveryAuthenticationFactorService.cs" "EnsurePolicyAllowedAsync"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnRegistrationService.cs" "EnsurePolicyAllowedAsync"
Require-Text "src/IdentityAccess.Mfa.WebAuthn/WebAuthnAuthenticationService.cs" "EnsurePolicyAllowedAsync"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlUserAuthenticatorStore.cs" "FOR UPDATE;"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlUserAuthenticatorStore.cs" "WouldViolateRequiredMfa"
Require-Text "src/IdentityAccess.Application/Authentication/Mfa/MfaAdministrationService.cs" "RevokeAuthenticatorForRecoveryAsync"
Require-Text "src/IdentityAccess.Application/Authentication/Mfa/MfaAdministrationService.cs" "RevokeUserSessionsAsync"
Require-Text "src/IdentityAccess.Api/Controllers/MfaAdministrationController.cs" "recovery-revoke"
Require-Text "src/IdentityAccess.Api/Controllers/MfaAdministrationController.cs" "GetUserSecurityState"
Require-Text "clients/typescript/src/client/administration/IdentityAccessMfaClient.ts" "getUserSecurityState"
Require-Text "clients/typescript/src/client/administration/IdentityAccessMfaClient.ts" "revokeAuthenticatorForRecovery"
Require-Text "clients/typescript/src/client/administration/IdentityAccessAdministrationTransport.ts" "postAction<T>"
Require-Text "examples/nextjs/admin/app/identity/mfa/page.tsx" "Inspect effective MFA state"
Require-Text "clients/typescript/test/client.test.mjs" "revokeAuthenticatorForRecovery"
Require-Text "scripts/verify.ps1" "verify-mfa-integration-source-consistency.ps1"

Write-Host "MFA integration source consistency validation passed."
