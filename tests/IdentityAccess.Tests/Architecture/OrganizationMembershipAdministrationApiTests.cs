using IdentityAccess.Api.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the explicit OrganizationMembership HTTP contract.</summary>
    public sealed class OrganizationMembershipAdministrationApiTests
    {
        [Fact]
        public void Organization_membership_capability_is_stable()
        {
            Assert.Equal(
                "organization-membership",
                IdentityAccessAdministrationCapabilities.OrganizationMemberships);
        }

        [Fact]
        public void Organization_membership_controllers_are_tenant_scoped_and_capability_protected()
        {
            var root = FindRepositoryRoot();
            var controllerFiles = new[]
            {
                "OrganizationMembershipsController.cs",
                "TenantMembershipOrganizationsController.cs"
            };

            foreach (var controllerFile in controllerFiles)
            {
                var source = File.ReadAllText(
                    Path.Combine(
                        root,
                        "src",
                        "IdentityAccess.Api",
                        "Controllers",
                        controllerFile));

                Assert.Contains(
                    "tenants/{tenantId:guid}",
                    source,
                    StringComparison.Ordinal);

                Assert.Contains(
                    "IdentityAccessAdministrationCapabilities.OrganizationMemberships",
                    source,
                    StringComparison.Ordinal);

                Assert.DoesNotContain(
                    "AllowAnonymous",
                    source,
                    StringComparison.Ordinal);
            }
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
