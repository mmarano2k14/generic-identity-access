using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents the response payload for password login.</summary>
    public sealed record PasswordLoginResponse(
        Guid UserId,
        Guid SessionId,
        string SessionToken,
        DateTimeOffset ExpiresAt,
        string RedirectUri,
        AuthenticationAssuranceResponse Assurance);
}
