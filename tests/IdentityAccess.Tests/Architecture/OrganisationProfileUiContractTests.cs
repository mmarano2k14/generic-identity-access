namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the OrganisationProfile application-UI boundary and focused component split.</summary>
    public sealed class OrganisationProfileUiContractTests
    {
        [Fact]
        public void Profile_workspace_is_outside_identity_navigation_and_reuses_organization_identity()
        {
            var root = FindRepositoryRoot();
            var page = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "app",
                "organisations",
                "[organizationId]",
                "profile",
                "page.tsx");

            Assert.Contains("OrganisationProfileWorkspaceService", page, StringComparison.Ordinal);
            Assert.Contains("Organization identity is not editable here", page, StringComparison.Ordinal);
            Assert.DoesNotContain("createOrganizationAction", page, StringComparison.Ordinal);
            Assert.DoesNotContain("updateOrganizationAction", page, StringComparison.Ordinal);

            var navigation = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "components",
                "AdminNavigation.tsx");

            Assert.DoesNotContain("organisation-profile", navigation, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Profile_panel_is_composed_from_focused_ui_components()
        {
            var root = FindRepositoryRoot();
            var panel = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "components",
                "organisation-profile",
                "OrganisationProfilePanel.tsx");

            foreach (var marker in new[]
            {
                "ProfileTemplateSelector",
                "DomainCompositionPanel",
                "ProfileVersionPanel",
                "ProfileLifecycleActions"
            })
            {
                Assert.Contains(marker, panel, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Template_and_domain_foreign_references_are_selected_from_protected_catalog_reads()
        {
            var root = FindRepositoryRoot();
            var workspace = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "OrganisationProfileWorkspaceService.ts");

            Assert.Contains("organisationProfileTemplates.list", workspace, StringComparison.Ordinal);
            Assert.Contains("organisationProfileTemplateVersions.list", workspace, StringComparison.Ordinal);
            Assert.Contains("version.status === 2", workspace, StringComparison.Ordinal);

            var mutation = Read(
                root,
                "examples",
                "nextjs",
                "admin",
                "server",
                "OrganisationProfileMutationService.ts");

            Assert.Contains("active published catalog reference", mutation, StringComparison.Ordinal);
            Assert.Contains("published selectable reference", mutation, StringComparison.Ordinal);
            Assert.DoesNotContain("administration.organizations.create", mutation, StringComparison.Ordinal);
            Assert.DoesNotContain("administration.organizations.update", mutation, StringComparison.Ordinal);
        }

        private static string Read(string root, params string[] parts) =>
            File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
