using IdentityAccess.Application;
using IdentityAccess.Application.Routing;
using IdentityAccess.Infrastructure.ConfigurationRouting;

namespace IdentityAccess.Api;

internal static class RoutingServiceRegistration
{
    public static void AddIdentityRouting(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection("IdentityAccess:Routing");
        var providerName = section["Provider"];
        var filePath = section["FilePath"];
        if (section.Value is not null || section.GetChildren().Any(child =>
            !string.Equals(child.Key, "Provider", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(child.Key, "FilePath", StringComparison.OrdinalIgnoreCase)))
            throw new RoutingConfigurationException(RoutingConfigurationFailure.ConflictingProviderSelection);

        // Missing configuration remains visibly unconfigured. It never selects a default database.
        if (providerName is null or "none")
        {
            if (filePath is not null)
                throw new RoutingConfigurationException(RoutingConfigurationFailure.ConflictingProviderSelection);
            builder.Services.AddSingleton(new FoundationStatus());
            return;
        }
        if (providerName != "configuration")
            throw new RoutingConfigurationException(RoutingConfigurationFailure.UnsupportedProvider);
        if (string.IsNullOrWhiteSpace(filePath))
            throw new RoutingConfigurationException(RoutingConfigurationFailure.InvalidValue);

        string absolutePath;
        try { absolutePath = Path.GetFullPath(filePath, builder.Environment.ContentRootPath); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            throw new RoutingConfigurationException(RoutingConfigurationFailure.FileUnavailable);
        }
        var provider = ConfigurationRoutingProvider.LoadFile(absolutePath);
        builder.Services.AddSingleton<IDatabaseRouteResolver>(provider);
        builder.Services.AddSingleton<IAuthenticationDirectoryLocator>(provider);
        builder.Services.AddSingleton(new FoundationStatus(databaseRoutingConfigured: true));
    }
}
