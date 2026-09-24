namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class PostgreSqlMfaIntegrationHardeningContractTests
    {
        [Fact]
        public void Generic_authenticator_revocation_locks_the_users_factor_set_before_required_policy_check()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Authentication",
                "PostgreSqlUserAuthenticatorStore.cs"));

            Assert.Contains("AND user_id = @user_id", source, StringComparison.Ordinal);
            Assert.Contains("FOR UPDATE;", source, StringComparison.Ordinal);
            Assert.Contains("WouldViolateRequiredMfa", source, StringComparison.Ordinal);
            Assert.Contains("row_version = @expected_version", source, StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                    return current.FullName;
                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
