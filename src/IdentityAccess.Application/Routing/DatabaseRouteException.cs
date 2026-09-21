namespace IdentityAccess.Application.Routing;

public enum DatabaseRouteFailure
{
    RouteNotFound = 1,
    RouteDisabled,
    DestinationDisabled,
    AuthenticationContextNotFound,
    AuthenticationContextDisabled,
    ApplicationContextMismatch
}

/// <summary>Controlled placement failure, separate from an authorization denial.</summary>
public sealed class DatabaseRouteException : Exception
{
    public DatabaseRouteFailure Code { get; }

    public DatabaseRouteException(DatabaseRouteFailure code)
        : base($"Database routing failed ({code}).")
    {
        if (!Enum.IsDefined(code))
            throw new ArgumentOutOfRangeException(nameof(code));
        Code = code;
    }
}
