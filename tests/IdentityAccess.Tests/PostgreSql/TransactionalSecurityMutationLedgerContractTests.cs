namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>
    /// Protects transactional security-mutation ledger coverage, append-only behavior, and
    /// secret-safe record-key capture.
    /// </summary>
    public sealed class TransactionalSecurityMutationLedgerContractTests
    {
        /// <summary>
        /// Verifies the migration attaches mutation triggers to credential/session, identity,
        /// policy, resource-scope, and identity-scope authority tables.
        /// </summary>
        [Fact]
        public void Ledger_covers_security_sensitive_persistence_tables()
        {
            var source = Migration();

            foreach (var table in new[]
            {
                "users",
                "tenants",
                "tenant_memberships",
                "user_groups",
                "group_memberships",
                "application_security_models",
                "application_capabilities",
                "permission_policies",
                "policy_statements",
                "group_policy_bindings",
                "password_credentials",
                "user_sessions",
                "application_scope_types",
                "resource_scopes",
                "identity_scope_administration_groups",
                "identity_scope_administration_group_memberships",
                "identity_scope_administration_policies",
                "identity_scope_administration_policy_statements",
                "identity_scope_administration_group_policy_bindings"
            })
            {
                Assert.Contains(
                    $"ON identity_access.{table}",
                    source,
                    StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies credential/session triggers capture identifiers only and never secret-bearing
        /// columns.
        /// </summary>
        [Fact]
        public void Credential_and_session_ledger_keys_exclude_secret_columns()
        {
            var source = Migration();

            Assert.Contains(
                "trg_security_mutation_password_credentials",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "trg_security_mutation_user_sessions",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "'password_hash'",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "'token_hash'",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "'connection_string'",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>Verifies the mutation ledger rejects update and delete operations.</summary>
        [Fact]
        public void Mutation_ledger_is_append_only()
        {
            var source = Migration();

            Assert.Contains(
                "BEFORE UPDATE OR DELETE",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "security mutation events are append-only",
                source,
                StringComparison.Ordinal);
        }

        private static string Migration()
        {
            var root = FindRepositoryRoot();

            return File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Infrastructure.PostgreSql",
                    "Migrations",
                    "0011_transactional_security_mutation_ledger.sql"));
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
