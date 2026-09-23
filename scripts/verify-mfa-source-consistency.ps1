$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required MFA source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "MFA source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "MFA source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Domain/AuthenticationFactorProviderKey.cs",
    "src/IdentityAccess.Domain/MfaPolicy.cs",
    "src/IdentityAccess.Domain/MfaPolicyMode.cs",
    "src/IdentityAccess.Domain/UserAuthenticator.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/IAuthenticationFactorProvider.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/IAuthenticationFactorProviderRegistry.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/IMfaAdministrationService.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/IMfaPolicyStore.cs",
    "src/IdentityAccess.Application/Authentication/Mfa/IUserAuthenticatorStore.cs",
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationFactorProviderRegistry.cs",
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationFactorProviderServiceCollectionExtensions.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlMfaPolicyStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlUserAuthenticatorStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql",
    "src/IdentityAccess.Api/Controllers/MfaAdministrationController.cs",
    "clients/typescript/src/client/administration/IdentityAccessMfaClient.ts",
    "examples/nextjs/admin/app/identity/mfa/page.tsx",
    "scripts/postgresql/verify-mfa-provider-foundation.ps1"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "identity_access.mfa_policies"
Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "identity_access.mfa_policy_providers"
Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "identity_access.user_authenticators"
Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "trg_security_mutation_user_authenticators"
Reject-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "provider_payload"
Reject-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "totp_secret"
Reject-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "private_key"
Reject-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" `
    "recovery_code"

Require-Text `
    "src/IdentityAccess.Application/Authentication/Mfa/IAuthenticationFactorProvider.cs" `
    "AuthenticationFactorProviderDescriptor Descriptor"
Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationFactorProviderRegistry.cs" `
    "registered more than once"
Require-Text `
    "src/IdentityAccess.Application/Authentication/Mfa/MfaAdministrationService.cs" `
    "AuthenticationFactorProviderNotRegisteredException"
Require-Text `
    "src/IdentityAccess.Application/Authentication/Mfa/MfaAdministrationService.cs" `
    "auditWriter.TryWriteAsync("
Reject-Text `
    "src/IdentityAccess.Application/Authentication/Mfa/MfaAdministrationService.cs" `
    "auditWriter.WriteAsync("
Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "hasPolicyStore != hasAuthenticatorStore"
Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "AddMfaAdministration(services)"
Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationServiceCollectionExtensions.cs" `
    "public static IServiceCollection AddIdentityAccessAuthenticationFactorProvider<TProvider>("
Require-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationFactorProviderServiceCollectionExtensions.cs" `
    "internal static class AuthenticationFactorProviderServiceCollectionExtensions"
Reject-Text `
    "src/IdentityAccess.Infrastructure.Authentication/AuthenticationFactorProviderServiceCollectionExtensions.cs" `
    "public static class AuthenticationFactorProviderServiceCollectionExtensions"
Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlUserAuthenticatorStore.cs" `
    "DateTimeOffset? confirmedAt ="
Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlUserAuthenticatorStore.cs" `
    "DateTimeOffset? lastUsedAt ="
Require-Text `
    "src/IdentityAccess.Infrastructure.PostgreSql/Authentication/PostgreSqlUserAuthenticatorStore.cs" `
    "DateTimeOffset? revokedAt ="
Require-Text `
    "src/IdentityAccess.Api/Controllers/MfaAdministrationController.cs" `
    "MfaPolicies"
Require-Text `
    "src/IdentityAccess.Api/Controllers/MfaAdministrationController.cs" `
    "MfaAuthenticators"
Require-Text `
    "clients/typescript/src/client/administration/IdentityAccessAdministrationClient.ts" `
    "IdentityAccessMfaClient"
Require-Text `
    "clients/typescript/src/admin-ui-builder.ts" `
    "withMfa"

$productionMfaSources = Get-ChildItem `
    (Join-Path $repositoryRoot "src") `
    -Filter "*.cs" `
    -Recurse `
    -File | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and
        $_.FullName -notmatch '[\\/]IdentityAccess\.Mfa\.[^\\/]+[\\/]'
    }

foreach ($sourceFile in $productionMfaSources) {
    $source = Get-Content $sourceFile.FullName -Raw
    if ($source -match '"totp"|"webauthn"') {
        throw "Generic MFA production source hard-codes a concrete provider: $($sourceFile.FullName)"
    }
}

Write-Host "MFA source consistency validation passed."
