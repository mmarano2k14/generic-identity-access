namespace IdentityAccess.Tests.PostgreSql
{
    /// <summary>
    /// Verifies PostgreSQL session lifecycle SQL preserves current account-state guarantees.
    /// </summary>
    public sealed class PostgreSqlSessionLifecycleContractTests
    {
        /// <summary>
        /// Verifies session issuance and validation require an active current user.
        /// </summary>
        [Fact]
        public void Session_store_checks_current_active_user_state()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Authentication",
                "PostgreSqlAuthenticationSessionStore.cs");

            Assert.Contains(
                "u.status = @active_user_status",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "INNER JOIN identity_access.users AS u",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "CreateForActiveUserAsync",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies user suspension revokes existing sessions in the same SQL command.
        /// </summary>
        [Fact]
        public void User_status_update_revokes_sessions_when_account_becomes_inactive()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Directory",
                "PostgreSqlUserDirectoryStore.cs");

            Assert.Contains(
                "UPDATE identity_access.user_sessions",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "@status <> @active_user_status",
                source,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies password changes revoke subject sessions only when the credential update
        /// succeeds.
        /// </summary>
        [Fact]
        public void Password_change_revokes_sessions_only_after_successful_credential_update()
        {
            var source = Read(
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Authentication",
                "PostgreSqlCredentialMutationStore.cs");

            Assert.Contains(
                "UPDATE identity_access.user_sessions",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "EXISTS (SELECT 1 FROM updated)",
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
