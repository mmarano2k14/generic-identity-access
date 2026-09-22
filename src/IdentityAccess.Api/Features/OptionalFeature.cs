using System.Diagnostics.CodeAnalysis;

namespace IdentityAccess.Api.Features
{
    /// <summary>
    /// Represents an optionally configured application feature without requiring controllers
    /// to resolve dependencies through <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TService">The feature service contract.</typeparam>
    public sealed class OptionalFeature<TService>
        where TService : class
    {
        private readonly TService? service;

        internal OptionalFeature(TService? service)
        {
            this.service = service;
        }

        /// <summary>
        /// Gets a value indicating whether the feature service is available on the current host.
        /// </summary>
        public bool IsAvailable => service is not null;

        /// <summary>
        /// Attempts to obtain the configured feature service.
        /// </summary>
        /// <param name="value">The configured service when available; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the feature is available; otherwise <see langword="false"/>.</returns>
        public bool TryGet([NotNullWhen(true)] out TService? value)
        {
            value = service;
            return value is not null;
        }
    }
}
