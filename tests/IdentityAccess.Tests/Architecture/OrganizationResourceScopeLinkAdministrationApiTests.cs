using IdentityAccess.Api.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the Organization ResourceScope-link HTTP contract.</summary>
    public sealed class OrganizationResourceScopeLinkAdministrationApiTests
    {
        [Fact]
        public void Resource_scope_link_controller_is_tenant_and_application_scoped()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Api",
                    "Controllers",
                    "OrganizationResourceScopeLinksController.cs"));

            Assert.Contains(
                "applications/{applicationKey}",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "tenants/{tenantId:guid}",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                "IdentityAccessAdministrationCapabilities.OrganizationScopeLinks",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "IdentityAccessAdministrationCapabilities.Organizations",
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "AllowAnonymous",
                source,
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
