using IdentityAccess.Api.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the identity-scope authority administration API surface.</summary>
    public sealed class IdentityScopeAuthorityAdministrationApiTests
    {
        /// <summary>Verifies scope-authority controllers remain scope-level and use dedicated capabilities.</summary>
        [Fact]
        public void Scope_authority_controllers_use_scope_routes_and_dedicated_capabilities()
        {
            var root = FindRepositoryRoot();
            var controllerDirectory = Path.Combine(root, "src", "IdentityAccess.Api", "Controllers");

            var files = Directory
                .EnumerateFiles(controllerDirectory, "IdentityScopeAdministration*Controller.cs")
                .ToArray();

            Assert.Equal(4, files.Length);

            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                Assert.Contains("/applications/{applicationKey}/scope-authority/", source, StringComparison.Ordinal);
                Assert.DoesNotContain("/tenants/{tenantId", source, StringComparison.Ordinal);
            }

            Assert.Equal("scope-authority-group",
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityGroups);
            Assert.Equal("scope-authority-membership",
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityMemberships);
            Assert.Equal("scope-authority-policy",
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityPolicies);
            Assert.Equal("scope-authority-statement",
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityStatements);
            Assert.Equal("scope-authority-binding",
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityBindings);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln")))
                    return current.FullName;

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
