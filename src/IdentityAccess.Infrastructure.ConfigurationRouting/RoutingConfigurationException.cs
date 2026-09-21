namespace IdentityAccess.Infrastructure.ConfigurationRouting;

public enum RoutingConfigurationFailure
{
    InvalidJson = 1,
    DuplicateJsonProperty,
    ConfigurationTooLarge,
    FileUnavailable,
    UnsupportedSchema,
    UnsupportedProvider,
    UnsupportedPlacementGranularity,
    InvalidValue,
    DuplicateDestination,
    DuplicateRoute,
    UnknownDestination,
    ConflictingScopePlacement,
    DuplicateAuthenticationContext,
    UnknownAuthenticationRoute,
    ConflictingProviderSelection
}

/// <summary>Deliberately contains no configuration input, file path, secret reference or inner exception.</summary>
public sealed class RoutingConfigurationException : Exception
{
    public RoutingConfigurationFailure Code { get; }

    public RoutingConfigurationException(RoutingConfigurationFailure code)
        : base($"Routing configuration rejected ({code}).")
    {
        if (!Enum.IsDefined(code))
            throw new ArgumentOutOfRangeException(nameof(code));
        Code = code;
    }
}
