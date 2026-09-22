using IdentityAccess.Api.Features;

namespace IdentityAccess.Tests.Api
{
    /// <summary>
    /// Verifies the controller-facing optional feature handle used for configurable services.
    /// </summary>
    public sealed class OptionalFeatureTests
    {
        /// <summary>
        /// Verifies that an unavailable feature reports its state without manufacturing a service.
        /// </summary>
        [Fact]
        public void Unavailable_feature_returns_false()
        {
            var feature = Create<string>(null);

            Assert.False(feature.IsAvailable);
            Assert.False(feature.TryGet(out var value));
            Assert.Null(value);
        }

        /// <summary>
        /// Verifies that an available feature returns the exact registered service instance.
        /// </summary>
        [Fact]
        public void Available_feature_returns_registered_service()
        {
            const string service = "configured";
            var feature = Create(service);

            Assert.True(feature.IsAvailable);
            Assert.True(feature.TryGet(out var value));
            Assert.Same(service, value);
        }

        private static OptionalFeature<TService> Create<TService>(TService? service)
            where TService : class
        {
            var constructor = typeof(OptionalFeature<TService>).GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Single();

            return (OptionalFeature<TService>)constructor.Invoke([service]);
        }
    }
}
