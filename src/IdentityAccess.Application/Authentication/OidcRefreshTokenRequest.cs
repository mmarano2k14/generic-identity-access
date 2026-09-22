namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents one public-client OAuth refresh-token grant request.</summary>
    /// <param name="ClientId">The registered public client identifier.</param>
    /// <param name="RefreshToken">The opaque refresh token presented by the client.</param>
    public sealed record OidcRefreshTokenRequest(
        string? ClientId,
        string? RefreshToken);
}
