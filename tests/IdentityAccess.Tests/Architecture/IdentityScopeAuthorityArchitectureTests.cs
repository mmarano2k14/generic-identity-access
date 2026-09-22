namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects identity-scope administration authority from tenant-authority conflation.
    /// </summary>
    public sealed class IdentityScopeAuthorityArchitectureTests
    {
        /// <summary>
        /// Verifies identity-scope grants live in dedicated scope tables and do not use a reserved
        /// or synthetic tenant identifier.
        /// </summary>
        [Fact]
        public void Identity_scope_authority_has_no_synthetic_tenant()
        {
            var migration = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0010_identity_scope_administration_authority.sql");

            Assert.Contains(
                "identity_scope_administration_groups",
                migration,
                StringComparison.Ordinal);

            Assert.Contains(
                "identity_scope_administration_policies",
                migration,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "tenant_id",
                migration,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies scope authorization still delegates wildcard decisions to the external RBAC
        /// boundary rather than evaluating patterns locally.
        /// </summary>
        [Fact]
        public void Identity_scope_authorization_delegates_wildcard_decision()
        {
            var source = Read(
                "src",
                "IdentityAccess.Authorization",
                "IdentityScopeAuthorizationService.cs");

            Assert.Contains(
                "CapabilityGrantAuthorizationEvaluator",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "StartsWith",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "Split(':')",
                source,
                StringComparison.Ordinal);
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
