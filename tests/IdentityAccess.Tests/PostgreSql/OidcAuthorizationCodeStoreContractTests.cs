namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>Protects OIDC authorization-code persistence and single-use PKCE semantics.</summary>
    public sealed class OidcAuthorizationCodeStoreContractTests
    {
        /// <summary>
        /// Verifies code issuance and consumption both require current active session/user state.
        /// </summary>
        [Fact]
        public void Code_store_issues_and_consumes_against_current_session_state()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Authentication",
                "PostgreSqlOidcAuthorizationCodeStore.cs");

            Assert.Contains(
                "INSERT INTO identity_access.oidc_authorization_codes",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "FROM identity_access.user_sessions AS s",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "UPDATE identity_access.oidc_authorization_codes AS c",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "c.code_challenge = @code_challenge",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "c.consumed_at IS NULL",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "s.revoked_at IS NULL",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "u.status = @active_user_status",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>Verifies raw authorization codes are never persisted by the schema.</summary>
        [Fact]
        public void Authorization_code_schema_persists_hash_not_raw_code()
        {
            var migration = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0012_oidc_authorization_code_pkce.sql");

            Assert.Contains(
                "code_hash bytea",
                migration,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "\n    code text",
                migration,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "trg_security_mutation_oidc_authorization_codes",
                migration,
                StringComparison.Ordinal);
        }

        private static string Read(params string[] segments)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "IdentityAccess.sln")))
                {
                    return File.ReadAllText(
                        Path.Combine(
                            new[] { current.FullName }
                                .Concat(segments)
                                .ToArray()));
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
