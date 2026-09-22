using IdentityAccess.Rbac.MultiplexedAdapter;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the external RBAC integration boundary from regressing to per-request reflection
    /// discovery or losing deterministic context cleanup.
    /// </summary>
    public sealed class ExternalRbacAdapterHardeningTests
    {
        /// <summary>Verifies the adapter exposes a compatibility probe without external types.</summary>
        [Fact]
        public void Compatibility_probe_surface_contains_only_neutral_types()
        {
            var method = typeof(IMultiplexedRbacCompatibilityProbe)
                .GetMethod(
                    nameof(IMultiplexedRbacCompatibilityProbe.ProbeCompatibility));

            Assert.NotNull(method);
            Assert.Equal(
                typeof(MultiplexedRbacCompatibilityReport),
                method!.ReturnType);

            Assert.DoesNotContain(
                typeof(MultiplexedRbacCompatibilityReport)
                    .GetProperties(),
                property =>
                    property.PropertyType.Assembly
                        .GetName()
                        .Name?
                        .StartsWith(
                            "Multiplexed.",
                            StringComparison.Ordinal) == true);
        }

        /// <summary>
        /// Verifies reflection discovery is isolated in the binding loader and external execution
        /// context is cleared in a finally block.
        /// </summary>
        [Fact]
        public void Reflection_discovery_and_context_cleanup_are_centralized()
        {
            var root = FindRepositoryRoot();

            var adapter = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Rbac.MultiplexedAdapter",
                    "MultiplexedRbacAuthorizationAdapter.cs"));

            var loader = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Rbac.MultiplexedAdapter",
                    "MultiplexedRbacBindingLoader.cs"));

            Assert.Contains(
                "Lazy<MultiplexedRbacBindingResult>",
                adapter,
                StringComparison.Ordinal);

            Assert.Contains(
                "finally",
                adapter,
                StringComparison.Ordinal);

            Assert.Contains(
                "AccessorClearMethod.Invoke",
                adapter,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                ".GetProperty(",
                adapter,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                ".GetMethod(",
                adapter,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                ".GetConstructor(",
                adapter,
                StringComparison.Ordinal);

            Assert.Contains(
                "RequiredWritableProperty",
                loader,
                StringComparison.Ordinal);

            Assert.Contains(
                "RequiredMethod",
                loader,
                StringComparison.Ordinal);

            Assert.Contains(
                "RequiredCompatibleConstructor",
                loader,
                StringComparison.Ordinal);

            Assert.Contains(
                "EnsureLoadedFromExpectedPath",
                loader,
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
