namespace IdentityAccess.Tests.Architecture
{
    public sealed class ProductionQualificationContractTests
    {
        [Fact]
        public void Production_qualification_runs_repository_rbac_database_and_restore_gates()
        {
            var source = Read(
                "scripts",
                "verify-production-qualification.ps1");

            Assert.Contains("verify.ps1", source, StringComparison.Ordinal);
            Assert.Contains("verify-multiplexed-rbac.ps1", source, StringComparison.Ordinal);
            Assert.Contains("verify-migration-integrity.ps1", source, StringComparison.Ordinal);
            Assert.Contains("verify-atomic-mutations.ps1", source, StringComparison.Ordinal);
            Assert.Contains("verify-oidc-authorization-code.ps1", source, StringComparison.Ordinal);
            Assert.Contains("verify-oidc-refresh-token.ps1", source, StringComparison.Ordinal);
            Assert.Contains("verify-backup-restore.ps1", source, StringComparison.Ordinal);
            Assert.Contains("SkipBackupRestore", source, StringComparison.Ordinal);
        }

        [Fact]
        public void Backup_restore_qualification_uses_isolated_scratch_database_and_cleanup()
        {
            var source = Read(
                "scripts",
                "postgresql",
                "verify-backup-restore.ps1");

            Assert.Contains("pg_dump", source, StringComparison.Ordinal);
            Assert.Contains("pg_restore", source, StringComparison.Ordinal);
            Assert.Contains("createdb", source, StringComparison.Ordinal);
            Assert.Contains("dropdb", source, StringComparison.Ordinal);
            Assert.Contains("generic_identity_access_restore_", source, StringComparison.Ordinal);
            Assert.Contains("finally", source, StringComparison.Ordinal);
            Assert.Contains("verify-migration-integrity.ps1", source, StringComparison.Ordinal);
            Assert.Contains("validate-restored-database.sql", source, StringComparison.Ordinal);
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
