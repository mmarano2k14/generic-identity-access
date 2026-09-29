namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects focused Organization Directory composition in the integrated administration UI.</summary>
    public sealed class OrganizationDirectorySeparationTests
    {
        [Fact]
        public void Organization_mutations_are_split_by_responsibility()
        {
            var root = FindRepositoryRoot();

            var genericService = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminMutationService.ts");
            var organizationService = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminOrganizationMutationService.ts");
            var membershipService = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminOrganizationMembershipMutationService.ts");
            var scopeLinkService = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminOrganizationScopeLinkMutationService.ts");

            Assert.DoesNotContain(
                ".administration.organizations",
                genericService,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationMemberships",
                genericService,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationResourceScopeLinks",
                genericService,
                StringComparison.Ordinal);

            Assert.Contains(
                ".administration.organizations",
                organizationService,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationMemberships",
                organizationService,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationResourceScopeLinks",
                organizationService,
                StringComparison.Ordinal);

            Assert.Contains(
                ".administration.organizationMemberships",
                membershipService,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationResourceScopeLinks",
                membershipService,
                StringComparison.Ordinal);

            Assert.Contains(
                ".administration.organizationResourceScopeLinks",
                scopeLinkService,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationMemberships",
                scopeLinkService,
                StringComparison.Ordinal);

            Assert.InRange(LineCount(root, "IdentityAccessAdminOrganizationMutationService.ts"), 1, 230);
            Assert.InRange(LineCount(root, "IdentityAccessAdminOrganizationMembershipMutationService.ts"), 1, 220);
            Assert.InRange(LineCount(root, "IdentityAccessAdminOrganizationScopeLinkMutationService.ts"), 1, 180);
        }

        [Fact]
        public void Membership_and_organization_read_models_are_separate()
        {
            var root = FindRepositoryRoot();
            var membership = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminMembershipOverviewService.ts");
            var organizations = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminOrganizationOverviewService.ts");

            Assert.DoesNotContain(
                ".administration.organizations",
                membership,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.organizationMemberships",
                membership,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "organization-scope-link",
                membership,
                StringComparison.Ordinal);

            Assert.Contains(
                ".administration.organizations",
                organizations,
                StringComparison.Ordinal);
            Assert.Contains(
                ".administration.organizationMemberships",
                organizations,
                StringComparison.Ordinal);
            Assert.Contains(
                "organization-scope-link",
                organizations,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                ".administration.groups",
                organizations,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ".administration.tenantGroupAssignments",
                organizations,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Organization_directory_ui_is_split_into_focused_components()
        {
            var root = FindRepositoryRoot();
            var panelPath = Path.Combine(
                root,
                "examples",
                "nextjs",
                "admin",
                "components",
                "AdminOrganizationDirectoryPanel.tsx");
            var panel = File.ReadAllText(panelPath);

            Assert.Contains(
                "AdminOrganizationCreateDialog",
                panel,
                StringComparison.Ordinal);
            Assert.Contains(
                "AdminOrganizationTable",
                panel,
                StringComparison.Ordinal);
            Assert.Contains(
                "AdminOrganizationScopeLinkPanel",
                panel,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "AdminMutationDialog",
                panel,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "../app/identity/actions",
                panel,
                StringComparison.Ordinal);

            var table = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "components",
                "AdminOrganizationTable.tsx");

            Assert.Contains(
                "AdminOrganizationRowActions",
                table,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "AdminMutationDialog",
                table,
                StringComparison.Ordinal);

            Assert.InRange(
                File.ReadAllLines(panelPath).Length,
                1,
                140);
        }

        [Fact]
        public void Organization_directory_remains_inside_memberships()
        {
            var root = FindRepositoryRoot();

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
                        "page.tsx")));

            var membershipsPage = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "app",
                "identity",
                "memberships",
                "page.tsx");

            Assert.Contains(
                "IdentityAccessAdminMembershipOverviewService",
                membershipsPage,
                StringComparison.Ordinal);
            Assert.Contains(
                "IdentityAccessAdminOrganizationOverviewService",
                membershipsPage,
                StringComparison.Ordinal);
        }


        [Fact]
        public void Organization_scope_link_actions_match_the_focused_service_contract()
        {
            var root = FindRepositoryRoot();

            var actions = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "app",
                "identity",
                "actions.ts");

            var service = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "IdentityAccessAdminOrganizationScopeLinkMutationService.ts");

            Assert.Contains(
                "(service) => service.link(formData)",
                actions,
                StringComparison.Ordinal);

            Assert.Contains(
                "(service) => service.relink(formData)",
                actions,
                StringComparison.Ordinal);

            Assert.Contains(
                "(service) => service.unlink(formData)",
                actions,
                StringComparison.Ordinal);

            Assert.Contains(
                "public async link(formData: FormData)",
                service,
                StringComparison.Ordinal);

            Assert.Contains(
                "public async relink(formData: FormData)",
                service,
                StringComparison.Ordinal);

            Assert.Contains(
                "public async unlink(formData: FormData)",
                service,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "service.linkOrganizationResourceScope",
                actions,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "service.relinkOrganizationResourceScope",
                actions,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "service.unlinkOrganizationResourceScope",
                actions,
                StringComparison.Ordinal);
        }

        private static int LineCount(string root, string file) =>
            File.ReadAllLines(
                Path.Combine(
                    root,
                    "examples",
                    "nextjs",
                    "admin",
                    "server",
                    file))
                .Length;

        private static string Read(string root, params string[] parts) =>
            File.ReadAllText(
                Path.Combine(
                    new[] { root }.Concat(parts).ToArray()));

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
