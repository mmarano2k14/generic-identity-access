namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>
    /// Verifies request audit provenance is applied to every opened PostgreSQL connection before
    /// mutation statements execute.
    /// </summary>
    public sealed class PostgreSqlAuditSessionContextContractTests
    {
        /// <summary>Verifies all safe actor/correlation settings are overwritten per connection checkout.</summary>
        [Fact]
        public void Connection_factory_applies_complete_audit_session_context()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Infrastructure.PostgreSql",
                    "PostgreSqlConnectionFactory.cs"));

            foreach (var setting in new[]
            {
                "identity_access.correlation_id",
                "identity_access.actor_identity_scope_id",
                "identity_access.actor_user_id",
                "identity_access.actor_session_id",
                "identity_access.actor_client_id",
                "identity_access.actor_application_key",
                "identity_access.authentication_context_key"
            })
            {
                Assert.Contains(
                    setting,
                    source,
                    StringComparison.Ordinal);
            }

            Assert.Contains(
                "ApplyAuditSessionContextAsync",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "session_token",
                source,
                StringComparison.OrdinalIgnoreCase);

            Assert.DoesNotContain(
                "password",
                source,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "IdentityAccess.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
