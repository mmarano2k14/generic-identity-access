namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>Protects atomic scope-authority membership and binding persistence invariants.</summary>
    public sealed class IdentityScopeAuthorityAdministrationStoreContractTests
    {
        /// <summary>Verifies member creation requires active group and active user in one statement.</summary>
        [Fact]
        public void Membership_store_checks_active_group_and_user_in_one_statement()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Directory",
                "PostgreSqlIdentityScopeAdministrationMembershipStore.cs");

            Assert.Contains("WITH eligible AS", source, StringComparison.Ordinal);
            Assert.Contains("g.status = @active_group_status", source, StringComparison.Ordinal);
            Assert.Contains("u.status = @active_user_status", source, StringComparison.Ordinal);
        }

        /// <summary>Verifies binding creation requires active group and active policy in one statement.</summary>
        [Fact]
        public void Binding_store_checks_active_group_and_policy_in_one_statement()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Directory",
                "PostgreSqlIdentityScopeAdministrationBindingStore.cs");

            Assert.Contains("WITH eligible AS", source, StringComparison.Ordinal);
            Assert.Contains("g.status = @active_group_status", source, StringComparison.Ordinal);
            Assert.Contains("p.status = @active_policy_status", source, StringComparison.Ordinal);
        }

        private static string Read(params string[] segments)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return File.ReadAllText(
                        Path.Combine(new[] { current.FullName }.Concat(segments).ToArray()));
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
