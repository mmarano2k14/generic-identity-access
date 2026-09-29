namespace OrganizationDirectory.Tests.Architecture
{
    /// <summary>Protects reusable Organization Directory layer boundaries and focused API adapters.</summary>
    public sealed class LayerSeparationTests
    {
        [Fact]
        public void Reusable_projects_do_not_reference_identity_access_projects()
        {
            var root = FindRepositoryRoot();

            foreach (var project in new[]
            {
                "OrganizationDirectory.Domain",
                "OrganizationDirectory.Contracts",
                "OrganizationDirectory.Application",
                "OrganizationDirectory.Infrastructure.PostgreSql"
            })
            {
                var path = Path.Combine(
                    root,
                    "src",
                    project,
                    $"{project}.csproj");

                var source = File.ReadAllText(path);

                Assert.DoesNotContain(
                    "IdentityAccess.",
                    source,
                    StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Api_controllers_depend_on_focused_organization_services()
        {
            var root = FindRepositoryRoot();

            AssertFocusedController(
                root,
                "OrganizationsController.cs",
                "IOrganizationAdministrationService",
                "IOrganizationMembershipAdministrationService",
                "IOrganizationResourceScopeLinkAdministrationService");

            AssertFocusedController(
                root,
                "OrganizationMembershipsController.cs",
                "IOrganizationMembershipAdministrationService",
                "IOrganizationAdministrationService",
                "IOrganizationResourceScopeLinkAdministrationService");

            AssertFocusedController(
                root,
                "OrganizationResourceScopeLinksController.cs",
                "IOrganizationResourceScopeLinkAdministrationService",
                "IOrganizationAdministrationService",
                "IOrganizationMembershipAdministrationService");
        }

        private static void AssertFocusedController(
            string root,
            string controller,
            string requiredService,
            string forbiddenServiceOne,
            string forbiddenServiceTwo)
        {
            var source = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Api",
                    "Controllers",
                    controller));

            Assert.Contains(
                requiredService,
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                forbiddenServiceOne,
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                forbiddenServiceTwo,
                source,
                StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "IdentityAccess.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Repository root containing IdentityAccess.sln was not found.");
        }
    }
}
