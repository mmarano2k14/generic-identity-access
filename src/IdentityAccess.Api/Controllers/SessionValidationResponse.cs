namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents the response payload for session validation.</summary>
    public sealed record SessionValidationResponse(
        Guid UserId,
        Guid SessionId,
        DateTimeOffset ExpiresAt,
        AuthenticationAssuranceResponse Assurance);
}
