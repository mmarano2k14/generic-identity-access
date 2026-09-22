using System.Text.RegularExpressions;

namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>Protects refresh-token hash persistence, rotation, replay revocation, and session linkage.</summary>
    public sealed class OidcRefreshTokenStoreContractTests
    {
        /// <summary>Verifies the store serializes mutations at family scope and checks current session state.</summary>
        [Fact]
        public void Refresh_store_serializes_family_rotation_and_checks_session_state()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Authentication",
                "PostgreSqlOidcRefreshTokenStore.cs");

            Assert.Contains(
                "pg_advisory_xact_lock",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "FROM identity_access.user_sessions AS s",
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

            Assert.Contains(
                "s.expires_at > @now",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "expiresAt <= now",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "revocation_reason = 'reuse_detected'",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "SET consumed_at = @now",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>Verifies UUID-like literals in the live refresh-token SQL fixture are valid UUIDs.</summary>
        [Fact]
        public void Refresh_validation_fixture_uses_valid_uuid_literals()
        {
            var source = Read(
                "scripts",
                "postgresql",
                "validate-oidc-refresh-token.sql");

            var matches = Regex.Matches(
                source,
                @"(?<![0-9A-Fa-f])([0-9A-Fa-f]+-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})(?![0-9A-Fa-f])");

            Assert.NotEmpty(matches);

            foreach (Match match in matches)
            {
                Assert.True(
                    Guid.TryParseExact(match.Groups[1].Value, "D", out _),
                    $"Invalid UUID fixture: {match.Groups[1].Value}");
            }
        }

        /// <summary>Verifies the schema persists only a 32-byte hash and uses safe ledger keys.</summary>
        [Fact]
        public void Refresh_schema_persists_hash_not_raw_token()
        {
            var migration = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0013_oidc_refresh_token_rotation.sql");

            Assert.Contains(
                "token_hash bytea",
                migration,
                StringComparison.Ordinal);

            Assert.Contains(
                "CHECK (octet_length(token_hash) = 32)",
                migration,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "raw_token",
                migration,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "trg_security_mutation_oidc_refresh_tokens",
                migration,
                StringComparison.Ordinal);

            Assert.Contains(
                "'identity_scope_id',\n    'token_id'",
                migration,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "'token_hash'",
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
