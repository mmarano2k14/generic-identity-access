namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents the result of logout.</summary>
    public sealed record LogoutResult(
        bool Revoked,
        string? PostLogoutRedirectUri = null,
        AuthenticationFailureCode? FailureCode = null);
}
