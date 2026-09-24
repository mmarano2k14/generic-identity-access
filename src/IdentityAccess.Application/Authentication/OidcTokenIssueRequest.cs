using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>Contains validated claims required to issue one OIDC token pair.</summary>
    /// <param name="Subject">The validated logical subject.</param>
    /// <param name="SessionId">The source local session identifier.</param>
    /// <param name="ClientId">The registered public client identifier.</param>
    /// <param name="Application">The registered application.</param>
    /// <param name="Scope">The granted canonical scope string.</param>
    /// <param name="Nonce">The OpenID Connect nonce.</param>
    /// <param name="AuthenticatedAt">The authentication time pinned to the authorization grant.</param>
    /// <param name="IssuedAt">The token issuance time.</param>
    /// <param name="Assurance">The authentication assurance pinned to the authorization grant.</param>
    public sealed record OidcTokenIssueRequest(
        SubjectReference Subject,
        Guid SessionId,
        string ClientId,
        ApplicationKey Application,
        string Scope,
        string Nonce,
        DateTimeOffset AuthenticatedAt,
        DateTimeOffset IssuedAt,
        AuthenticationAssurance? Assurance = null);
}
