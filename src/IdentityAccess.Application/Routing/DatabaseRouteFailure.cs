

namespace IdentityAccess.Application.Routing
{

    /// <summary>Defines failure codes for database route.</summary>
    public enum DatabaseRouteFailure
    {
        /// <summary>Indicates the route not found failure condition.</summary>
        RouteNotFound = 1,
        /// <summary>Indicates the route disabled failure condition.</summary>
        RouteDisabled,
        /// <summary>Indicates the destination disabled failure condition.</summary>
        DestinationDisabled,
        /// <summary>Indicates the authentication context not found failure condition.</summary>
        AuthenticationContextNotFound,
        /// <summary>Indicates the authentication context disabled failure condition.</summary>
        AuthenticationContextDisabled,
        /// <summary>Indicates the application context mismatch failure condition.</summary>
        ApplicationContextMismatch
    }

    /// <summary>Controlled placement failure, separate from an authorization denial.</summary>
}
