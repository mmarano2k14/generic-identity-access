

namespace IdentityAccess.Infrastructure.ConfigurationRouting
{

    /// <summary>Defines failure codes for routing configuration.</summary>
    public enum RoutingConfigurationFailure
    {
        /// <summary>Indicates the invalid JSON failure condition.</summary>
        InvalidJson = 1,
        /// <summary>Indicates the duplicate JSON property failure condition.</summary>
        DuplicateJsonProperty,
        /// <summary>Indicates the configuration too large failure condition.</summary>
        ConfigurationTooLarge,
        /// <summary>Indicates the file unavailable failure condition.</summary>
        FileUnavailable,
        /// <summary>Indicates the unsupported schema failure condition.</summary>
        UnsupportedSchema,
        /// <summary>Indicates the unsupported provider failure condition.</summary>
        UnsupportedProvider,
        /// <summary>Indicates the unsupported placement granularity failure condition.</summary>
        UnsupportedPlacementGranularity,
        /// <summary>Indicates the invalid value failure condition.</summary>
        InvalidValue,
        /// <summary>Indicates the duplicate destination failure condition.</summary>
        DuplicateDestination,
        /// <summary>Indicates the duplicate route failure condition.</summary>
        DuplicateRoute,
        /// <summary>Indicates the unknown destination failure condition.</summary>
        UnknownDestination,
        /// <summary>Indicates the conflicting scope placement failure condition.</summary>
        ConflictingScopePlacement,
        /// <summary>Indicates the duplicate authentication context failure condition.</summary>
        DuplicateAuthenticationContext,
        /// <summary>Indicates the unknown authentication route failure condition.</summary>
        UnknownAuthenticationRoute,
        /// <summary>Indicates the conflicting provider selection failure condition.</summary>
        ConflictingProviderSelection
    }

    /// <summary>Deliberately contains no configuration input, file path, secret reference or inner exception.</summary>
}
