using IdentityAccess.Api.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the Organization Directory administration HTTP contract.</summary>
    public sealed class OrganizationAdministrationApiTests
    {
        [Fact]
        public void Organization_capability_is_stable()
        {
            Assert.Equal("organization", IdentityAccessAdministrationCapabilities.Organizations);
        }

        [Fact]
        public void Organization_controller_is_tenant_scoped_and_capability_protected()
        {
            var root = FindRepositoryRoot();
            var controller = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Api",
                    "Controllers",
                    "OrganizationsController.cs"));

            Assert.Contains(
                "tenants/{tenantId:guid}/organizations",
                controller,
                StringComparison.Ordinal);

            Assert.Contains(
                "IdentityAccessAdministrationCapabilities.Organizations",
                controller,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "AllowAnonymous",
                controller,
                StringComparison.Ordinal);
        }

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
