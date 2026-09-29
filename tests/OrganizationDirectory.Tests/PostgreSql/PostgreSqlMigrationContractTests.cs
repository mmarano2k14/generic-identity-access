namespace OrganizationDirectory.Tests.PostgreSql
{
    public sealed class PostgreSqlMigrationContractTests
    {
        [Fact]
        public void Initial_organization_migration_contains_tenant_hierarchy_and_cycle_guards()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "src",
                "OrganizationDirectory.Infrastructure.PostgreSql",
                "Migrations",
                "0001_organizations.sql");

            var sql = File.ReadAllText(path);

            Assert.Contains("PRIMARY KEY (identity_scope_id, tenant_id, organization_id)", sql, StringComparison.Ordinal);
            Assert.Contains("UNIQUE (identity_scope_id, tenant_id, organization_key)", sql, StringComparison.Ordinal);
            Assert.Contains("CONSTRAINT fk_organizations_tenant", sql, StringComparison.Ordinal);
            Assert.Contains("CONSTRAINT fk_organizations_parent", sql, StringComparison.Ordinal);
            Assert.Contains("ON DELETE RESTRICT", sql, StringComparison.Ordinal);
            Assert.Contains("reject_organization_cycle", sql, StringComparison.Ordinal);
            Assert.Contains("row_version bigint NOT NULL DEFAULT 1", sql, StringComparison.Ordinal);
        }

        [Fact]
        public void Organization_membership_migration_references_identity_access_tenant_memberships()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "src",
                "OrganizationDirectory.Infrastructure.PostgreSql",
                "Migrations",
                "0002_organization_memberships.sql");

            var sql = File.ReadAllText(path);

            Assert.Contains(
                "organization_directory.organization_memberships",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "CONSTRAINT fk_organization_memberships_organization",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "CONSTRAINT fk_organization_memberships_tenant_membership",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "REFERENCES identity_access.tenant_memberships",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "(identity_scope_id, tenant_id, membership_id)",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "row_version bigint NOT NULL DEFAULT 1",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "ON DELETE RESTRICT",
                sql,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Resource_scope_link_migration_is_application_aware_and_references_identity_access()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "src",
                "OrganizationDirectory.Infrastructure.PostgreSql",
                "Migrations",
                "0003_organization_resource_scope_links.sql");

            var sql = File.ReadAllText(path);

            Assert.Contains(
                "organization_directory.organization_resource_scope_links",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "CONSTRAINT fk_organization_resource_scope_links_resource_scope",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "REFERENCES identity_access.resource_scopes",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "(identity_scope_id, tenant_id, application_key, resource_scope_id)",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "CONSTRAINT uq_organization_resource_scope_links_scope",
                sql,
                StringComparison.Ordinal);
            Assert.Contains(
                "row_version bigint NOT NULL DEFAULT 1",
                sql,
                StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "IdentityAccess.sln")))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new InvalidOperationException("Repository root was not found from the test output directory.");
        }
    }
}
