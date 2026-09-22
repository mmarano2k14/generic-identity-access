using IdentityAccess.Application.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{
    /// <summary>
    /// Registers the immutable file-based routing provider from trusted server configuration.
    /// </summary>
    public static class ConfigurationRoutingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds configuration-based routing when explicitly selected.
        /// </summary>
        /// <param name="services">The server service collection.</param>
        /// <param name="section">The <c>IdentityAccess:Routing</c> configuration section.</param>
        /// <param name="contentRootPath">The trusted server content root used for relative file paths.</param>
        /// <returns>The original service collection.</returns>
        public static IServiceCollection AddIdentityAccessConfigurationRouting(
            this IServiceCollection services,
            IConfigurationSection section,
            string contentRootPath)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

            var providerName = section["Provider"];
            var configuredFilePath = section["FilePath"];
            var filePath = string.IsNullOrWhiteSpace(configuredFilePath)
                ? null
                : configuredFilePath;

            if (section.GetChildren().Any(
                child =>
                    !string.Equals(
                        child.Key,
                        "Provider",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        child.Key,
                        "FilePath",
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new RoutingConfigurationException(
                    RoutingConfigurationFailure.ConflictingProviderSelection);
            }

            if (providerName is null or "none")
            {
                if (filePath is not null)
                {
                    throw new RoutingConfigurationException(
                        RoutingConfigurationFailure.ConflictingProviderSelection);
                }

                return services;
            }

            if (providerName != "configuration")
            {
                throw new RoutingConfigurationException(
                    RoutingConfigurationFailure.UnsupportedProvider);
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new RoutingConfigurationException(
                    RoutingConfigurationFailure.InvalidValue);
            }

            string absolutePath;

            try
            {
                absolutePath = Path.GetFullPath(filePath, contentRootPath);
            }
            catch (Exception error) when (
                error is ArgumentException or
                    NotSupportedException or
                    System.Security.SecurityException)
            {
                throw new RoutingConfigurationException(
                    RoutingConfigurationFailure.FileUnavailable);
            }

            var provider = ConfigurationRoutingProvider.LoadFile(absolutePath);

            services.AddSingleton<IDatabaseRouteResolver>(provider);
            services.AddSingleton<IAuthenticationDirectoryLocator>(provider);

            return services;
        }
    }
}
