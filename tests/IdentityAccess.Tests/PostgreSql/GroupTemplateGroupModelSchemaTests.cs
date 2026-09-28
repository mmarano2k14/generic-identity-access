namespace IdentityAccess.Tests.PostgreSql
{
    public sealed class GroupTemplateGroupModelSchemaTests
    {
        [Fact]
        public void Group_template_foundation_marks_existing_groups_instead_of_creating_another_catalog()
        {
            var root = FindRepositoryRoot();
            var path = Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Migrations",
                "0027_group_template_flag_foundation.sql");
            var sql = File.ReadAllText(path);

            Assert.Contains("ADD COLUMN IF NOT EXISTS is_template boolean NOT NULL DEFAULT FALSE", sql, StringComparison.Ordinal);
            Assert.Contains("ix_user_groups_template_catalog", sql, StringComparison.Ordinal);
            Assert.Contains("uq_user_groups_template_group_id", sql, StringComparison.Ordinal);
            Assert.Contains("WHERE is_template = TRUE", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("CREATE TABLE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("group_templates", sql, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln"))) return current.FullName;
                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
