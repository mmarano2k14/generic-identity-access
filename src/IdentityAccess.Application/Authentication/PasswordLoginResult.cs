namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents the result of password login.</summary>
    public sealed record PasswordLoginResult(
        PasswordLoginDecision Decision,
        Guid? UserId = null,
        Guid? SessionId = null,
        string? SessionToken = null,
        DateTimeOffset? ExpiresAt = null,
        string? RedirectUri = null,
        AuthenticationFailureCode? FailureCode = null);
}
