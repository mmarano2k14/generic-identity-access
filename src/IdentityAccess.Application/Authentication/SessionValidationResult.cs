namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents the result of server-side local-session validation.</summary>
    public sealed record SessionValidationResult(
        bool Valid,
        AuthenticatedSessionContext? Context = null);
}
