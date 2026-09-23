$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required recovery-code source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "Recovery-code source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $source = Get-Content (Require-File $RelativePath) -Raw
    if ($source.Contains($RejectedText)) {
        throw "Recovery-code source consistency failure in '$RelativePath': forbidden '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Mfa.Recovery/IdentityAccess.Mfa.Recovery.csproj",
    "src/IdentityAccess.Mfa.Recovery/RecoveryAuthenticationFactorProvider.cs",
    "src/IdentityAccess.Mfa.Recovery/RecoveryAuthenticationFactorService.cs",
    "src/IdentityAccess.Mfa.Recovery/RecoveryCodeGenerator.cs",
    "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0016_recovery_provider.sql",
    "tests/IdentityAccess.Tests/Mfa/Recovery/RecoveryCodeGeneratorTests.cs",
    "tests/IdentityAccess.Tests/Mfa/Recovery/RecoveryAuthenticationFactorServiceTests.cs",
    "scripts/postgresql/verify-recovery-provider.ps1",
    "docs/RECOVERY_PROVIDER.md"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Mfa.Recovery/RecoveryAuthenticationFactorProvider.cs" "AuthenticationFactorProviderCapabilities.Recovery"
Require-Text "src/IdentityAccess.Mfa.Recovery/RecoveryCodeGenerator.cs" "RandomNumberGenerator.GetInt32"
Require-Text "src/IdentityAccess.Mfa.Recovery/RecoveryCodeGenerator.cs" "SHA256.HashData"
Require-Text "src/IdentityAccess.Mfa.Recovery/RecoveryCodeProviderOptions.cs" "internal const int CodeEntropyBits = 80;"
Require-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "FOR UPDATE OF u, s"
Require-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "SELECT consumed_at"
Require-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "FOR UPDATE;"
Require-Text "src/IdentityAccess.Mfa.Recovery/PostgreSqlRecoveryCodeStore.cs" "consumed_at IS NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0016_recovery_provider.sql" "code_hash bytea NOT NULL"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0016_recovery_provider.sql" "CHECK (octet_length(code_hash) = 32)"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0016_recovery_provider.sql" "WHERE provider_key = 'recovery' AND status = 2"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0016_recovery_provider.sql" "raw_code"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0016_recovery_provider.sql" "protected_code"
Reject-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0014_mfa_provider_foundation.sql" "recovery_codes"
Require-Text "src/IdentityAccess.Api/Program.cs" "AddIdentityAccessRecoveryProvider("
Require-Text "IdentityAccess.sln" "IdentityAccess.Mfa.Recovery"
Require-Text "tests/IdentityAccess.Tests/IdentityAccess.Tests.csproj" "IdentityAccess.Mfa.Recovery.csproj"

Write-Host "Recovery-code source consistency validation passed."
