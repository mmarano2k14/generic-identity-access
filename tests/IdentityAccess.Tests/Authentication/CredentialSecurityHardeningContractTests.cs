using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Protects account-recovery and sensitive credential-change boundaries.</summary>
    public sealed class CredentialSecurityHardeningContractTests
    {
        [Fact]
        public void Sensitive_password_change_contract_requires_recent_mfa_window()
        {
            var options = new AuthenticationOptions();

            Assert.Equal(
                AuthenticationOptions.DefaultSensitiveOperationMfaMaxAgeMinutes,
                options.SensitiveOperationMfaMaxAgeMinutes);
            Assert.InRange(options.SensitiveOperationMfaMaxAgeMinutes, 1, 60);
        }

        [Fact]
        public void Password_change_revokes_sessions_and_refresh_tokens_atomically()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Authentication",
                "PostgreSqlCredentialMutationStore.cs");

            Assert.Contains("UPDATE identity_access.user_sessions", source, StringComparison.Ordinal);
            Assert.Contains("UPDATE identity_access.oidc_refresh_tokens", source, StringComparison.Ordinal);
            Assert.Contains("revocation_reason = 'credential_changed'", source, StringComparison.Ordinal);
            Assert.Contains("EXISTS (SELECT 1 FROM updated)", source, StringComparison.Ordinal);
        }

        [Fact]
        public void Recovery_reset_is_single_transaction_and_revokes_existing_credentials()
        {
            var source = Read(
                "src",
                "IdentityAccess.Mfa.Recovery",
                "PostgreSqlRecoveryCodeStore.cs");

            Assert.Contains("FOR UPDATE OF u, p, a, s", source, StringComparison.Ordinal);
            Assert.Contains("SET consumed_at = @occurred_at", source, StringComparison.Ordinal);
            Assert.Contains("UPDATE identity_access.password_credentials", source, StringComparison.Ordinal);
            Assert.Contains("UPDATE identity_access.user_sessions", source, StringComparison.Ordinal);
            Assert.Contains("UPDATE identity_access.oidc_refresh_tokens", source, StringComparison.Ordinal);
            Assert.Contains("revocation_reason = 'account_recovery'", source, StringComparison.Ordinal);
        }


        [Fact]
        public void Recovery_reset_resolves_the_active_recovery_authenticator_server_side()
        {
            var request = Read(
                "src",
                "IdentityAccess.Api",
                "Controllers",
                "RecoveryPasswordResetRequest.cs");
            var store = Read(
                "src",
                "IdentityAccess.Mfa.Recovery",
                "PostgreSqlRecoveryCodeStore.cs");

            Assert.DoesNotContain("AuthenticatorId", request, StringComparison.Ordinal);
            Assert.Contains("a.status = @active_status", store, StringComparison.Ordinal);
            Assert.Contains("a.authenticator_id", store, StringComparison.Ordinal);
        }

        [Fact]
        public void Refresh_token_schema_accepts_only_documented_security_revocation_reasons()
        {
            var migration = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0020_credential_security_hardening.sql");

            Assert.Contains("'reuse_detected'", migration, StringComparison.Ordinal);
            Assert.Contains("'credential_changed'", migration, StringComparison.Ordinal);
            Assert.Contains("'account_recovery'", migration, StringComparison.Ordinal);
        }

        private static string Read(params string[] segments)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return File.ReadAllText(
                        Path.Combine(
                            new[] { current.FullName }
                                .Concat(segments)
                                .ToArray()));
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
