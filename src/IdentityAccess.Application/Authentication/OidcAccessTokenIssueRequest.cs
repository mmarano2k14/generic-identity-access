using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>Contains validated claims required to issue one access token without an ID token.</summary>
    /// <param name="Subject">The validated logical subject.</param>
    /// <param name="SessionId">The source local session identifier.</param>
    /// <param name="ClientId">The registered public client identifier.</param>
    /// <param name="Application">The registered application.</param>
    /// <param name="Scope">The granted canonical scope string.</param>
    /// <param name="IssuedAt">The token issuance time.</param>
    /// <param name="Assurance">The authentication assurance pinned to the grant.</param>
    public sealed record OidcAccessTokenIssueRequest(
        SubjectReference Subject,
        Guid SessionId,
        string ClientId,
        ApplicationKey Application,
        string Scope,
        DateTimeOffset IssuedAt,
        AuthenticationAssurance? Assurance = null);
}
