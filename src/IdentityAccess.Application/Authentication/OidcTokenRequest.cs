namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents one OAuth authorization-code token exchange request.</summary>
    /// <param name="ClientId">The registered public client identifier.</param>
    /// <param name="GrantType">The requested OAuth grant type.</param>
    /// <param name="Code">The opaque authorization code.</param>
    /// <param name="RedirectUri">The exact redirect URI used during authorization.</param>
    /// <param name="CodeVerifier">The RFC 7636 PKCE verifier.</param>
    public sealed record OidcTokenRequest(
        string? ClientId,
        string? GrantType,
        string? Code,
        string? RedirectUri,
        string? CodeVerifier);
}
