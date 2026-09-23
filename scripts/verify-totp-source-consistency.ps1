$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required TOTP source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "TOTP source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "TOTP source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Mfa.Totp/IdentityAccess.Mfa.Totp.csproj",
    "src/IdentityAccess.Mfa.Totp/TotpAuthenticationFactorProvider.cs",
    "src/IdentityAccess.Mfa.Totp/TotpAuthenticationFactorService.cs",
    "src/IdentityAccess.Mfa.Totp/TotpCodeGenerator.cs",
    "src/IdentityAccess.Mfa.Totp/TotpSecretProtector.cs",
    "src/IdentityAccess.Mfa.Totp/PostgreSqlTotpAuthenticatorStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0015_totp_provider.sql",
    "tests/IdentityAccess.Tests/Mfa/Totp/TotpCodeGeneratorTests.cs",
    "tests/IdentityAccess.Tests/Mfa/Totp/TotpAuthenticationFactorServiceTests.cs",
    "scripts/postgresql/verify-totp-provider.ps1",
    "docs/TOTP_PROVIDER.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Mfa.Totp/TotpCodeGenerator.cs" "HMACSHA1.HashData"
Require-Text "src/IdentityAccess.Mfa.Totp/TotpProviderOptions.cs" "internal const int Digits = 6;"
Require-Text "src/IdentityAccess.Mfa.Totp/TotpProviderOptions.cs" "internal const int PeriodSeconds = 30;"
Require-Text "src/IdentityAccess.Mfa.Totp/TotpSecretProtector.cs" "CreateProtector("
Require-Text "src/IdentityAccess.Mfa.Totp/PostgreSqlTotpAuthenticatorStore.cs" "FOR UPDATE OF u, t"
Require-Text "src/IdentityAccess.Mfa.Totp/PostgreSqlTotpAuthenticatorStore.cs" "last_accepted_time_step"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0015_totp_provider.sql" "protected_secret bytea NOT NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0015_totp_provider.sql" "last_accepted_time_step bigint NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0015_totp_provider.sql" "REFERENCES identity_access.user_authenticators"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0015_totp_provider.sql" "raw_secret"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" "totp_authenticators"
Require-Text "src/IdentityAccess.Api/Program.cs" "AddIdentityAccessTotpProvider("
Require-Text "IdentityAccess.sln" "IdentityAccess.Mfa.Totp"
Require-Text "tests/IdentityAccess.Tests/IdentityAccess.Tests.csproj" "IdentityAccess.Mfa.Totp.csproj"

Write-Host "TOTP source consistency validation passed."
