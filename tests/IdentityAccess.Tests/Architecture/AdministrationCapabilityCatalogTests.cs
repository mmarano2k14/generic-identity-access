using IdentityAccess.Api.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Verifies the stable administration capability catalog and protects controllers from
    /// reintroducing duplicated raw capability literals.
    /// </summary>
    public sealed class AdministrationCapabilityCatalogTests
    {
        /// <summary>
        /// Verifies that every administration capability segment is compatible with the
        /// canonical concrete capability grammar.
        /// </summary>
        [Fact]
        public void Catalog_entries_are_valid_concrete_capability_segments()
        {
            var features = new[]
            {
                IdentityAccessAdministrationCapabilities.Users,
                IdentityAccessAdministrationCapabilities.Tenants,
                IdentityAccessAdministrationCapabilities.TenantMemberships,
                IdentityAccessAdministrationCapabilities.Groups,
                IdentityAccessAdministrationCapabilities.GroupMemberships,
                IdentityAccessAdministrationCapabilities.Credentials,
                IdentityAccessAdministrationCapabilities.Policies,
                IdentityAccessAdministrationCapabilities.PolicyStatements,
                IdentityAccessAdministrationCapabilities.PolicyBindings,
                IdentityAccessAdministrationCapabilities.ScopeTypes,
                IdentityAccessAdministrationCapabilities.ResourceScopes,
                IdentityAccessAdministrationCapabilities.Sessions,
                IdentityAccessAdministrationCapabilities.MfaPolicies,
                IdentityAccessAdministrationCapabilities.MfaAuthenticators,
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityGroups,
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityMemberships,
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityPolicies,
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityStatements,
                IdentityAccessAdministrationCapabilities.IdentityScopeAuthorityBindings
            };

            Assert.Equal(features.Length, features.Distinct(StringComparer.Ordinal).Count());

            foreach (var feature in features)
            {
                _ = new CapabilityKey(
                    IdentityAccessAdministrationCapabilities.Resource,
                    feature,
                    IdentityAccessAdministrationCapabilities.Read);

                _ = new CapabilityKey(
                    IdentityAccessAdministrationCapabilities.Resource,
                    feature,
                    IdentityAccessAdministrationCapabilities.Write);
            }
        }

        /// <summary>
        /// Verifies that administration controllers use the centralized capability catalog
        /// rather than repeating authorization literals.
        /// </summary>
        [Fact]
        public void Controllers_do_not_repeat_raw_administration_capability_literals()
        {
            var root = FindRepositoryRoot();
            var controllers = Path.Combine(root, "src", "IdentityAccess.Api", "Controllers");

            foreach (var file in Directory.EnumerateFiles(controllers, "*Controller.cs"))
            {
                var source = File.ReadAllText(file);
                Assert.DoesNotContain(
                    "RequireAdministrationCapability(\"identity-access\"",
                    source,
                    StringComparison.Ordinal);
            }
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
