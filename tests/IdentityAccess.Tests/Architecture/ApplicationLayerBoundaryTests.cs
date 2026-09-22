using IdentityAccess.Application.Routing;
using IdentityAccess.Contracts;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the application layer from transport-contract dependencies and removed profile
    /// projection surface.
    /// </summary>
    public sealed class ApplicationLayerBoundaryTests
    {
        /// <summary>
        /// Verifies that the application assembly does not reference the public transport-contract
        /// assembly.
        /// </summary>
        [Fact]
        public void Application_assembly_does_not_reference_contracts_assembly()
        {
            var references = typeof(IDatabaseRouteResolver)
                .Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ToArray();

            Assert.DoesNotContain(
                typeof(LivenessResponse).Assembly.GetName().Name,
                references,
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Verifies that the removed profile transport contracts are not exported by the contracts
        /// assembly.
        /// </summary>
        [Fact]
        public void Contracts_assembly_does_not_export_removed_profile_types()
        {
            var names = typeof(LivenessResponse)
                .Assembly
                .GetExportedTypes()
                .Select(type => type.Name)
                .ToArray();

            foreach (var removedName in new[]
            {
                "UserProfileResponse",
                "TenantProfileResponse",
                "GroupProfileResponse"
            })
            {
                Assert.DoesNotContain(removedName, names, StringComparer.Ordinal);
            }
        }
    }
}
