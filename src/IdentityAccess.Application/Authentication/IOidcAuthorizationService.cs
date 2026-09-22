namespace IdentityAccess.Application.Authentication
{
    /// <summary>
    /// Defines the OAuth 2.0 Authorization Code + PKCE / OpenID Connect protocol orchestration.
    /// </summary>
    public interface IOidcAuthorizationService
    {
        /// <summary>Gets provider endpoint metadata.</summary>
        OidcProviderMetadata Metadata { get; }

        /// <summary>Gets the active public signing key used for newly issued tokens.</summary>
        OidcJsonWebKey SigningKey { get; }

        /// <summary>Gets all public signing keys currently published through JWKS.</summary>
        IReadOnlyList<OidcJsonWebKey> SigningKeys { get; }

        /// <summary>Validates an authorization request and issues a one-time code.</summary>
        Task<OidcAuthorizationResult> AuthorizeAsync(
            OidcAuthorizationRequest request,
            AuthenticatedSessionContext? session,
            CancellationToken cancellationToken);

        /// <summary>Consumes an authorization code with PKCE and issues tokens plus a refresh-token family.</summary>
        Task<OidcTokenResult> ExchangeCodeAsync(
            OidcTokenRequest request,
            CancellationToken cancellationToken);

        /// <summary>Rotates a refresh token and issues a new access token.</summary>
        Task<OidcTokenResult> RefreshAsync(
            OidcRefreshTokenRequest request,
            CancellationToken cancellationToken);
    }
}
