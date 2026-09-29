namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the decision to embed Organization administration inside Identity Membership.</summary>
    public sealed class OrganizationMembershipUiIntegrationTests
    {
        [Fact]
        public void Organization_administration_is_embedded_in_memberships_not_a_separate_app()
        {
            var root = FindRepositoryRoot();
            var membershipsPage = File.ReadAllText(
                Path.Combine(
                    root,
                    "examples",
                    "nextjs",
                    "admin",
                    "app",
                    "identity",
                    "memberships",
                    "page.tsx"));

            Assert.Contains(
                "AdminOrganizationDirectoryPanel",
                membershipsPage,
                StringComparison.Ordinal);
            Assert.Contains(
                "AdminManageMemberOrganizationsDialog",
                membershipsPage,
                StringComparison.Ordinal);

            Assert.False(
                File.Exists(
                    Path.Combine(
                        root,
                        "examples",
                        "nextjs",
                        "admin",
                        "app",
                        "identity",
                        "organizations",
                        "page.tsx")),
                "Organization Directory must remain embedded in the existing Identity Membership workspace.");
        }

        [Fact]
        public void Organization_membership_copy_explains_that_belonging_is_not_permission()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(
                Path.Combine(
                    root,
                    "examples",
                    "nextjs",
                    "admin",
                    "components",
                    "AdminManageMemberOrganizationsDialog.tsx"));

            Assert.Contains(
                "It never grants groups, policies, capabilities, or RBAC authority",
                source,
                StringComparison.Ordinal);
        }


        [Fact]
        public void Organization_resource_scope_mutations_use_server_backed_entity_autocomplete()
        {
            var root = FindRepositoryRoot();

            var panel = File.ReadAllText(
                Path.Combine(
                    root,
                    "examples",
                    "nextjs",
                    "admin",
                    "components",
                    "AdminOrganizationScopeLinkPanel.tsx"));

            var overview = File.ReadAllText(
                Path.Combine(
                    root,
                    "examples",
                    "nextjs",
                    "admin",
                    "server",
                    "IdentityAccessAdminOrganizationOverviewService.ts"));

            Assert.Contains(
                "AdminEntityAutocomplete",
                panel,
                StringComparison.Ordinal);

            Assert.Contains(
                "name=\"resourceScopeId\"",
                panel,
                StringComparison.Ordinal);

            Assert.Contains(
                "kind=\"resource-scope\"",
                panel,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "AdminSelectField label=\"Resource scope\"",
                panel,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "resourceScopes.list(",
                overview,
                StringComparison.Ordinal);
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
