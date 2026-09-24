namespace IdentityAccess.Api.Controllers
{
    /// <summary>Contains one TOTP proof for local-session step-up.</summary>
    public sealed record TotpSessionStepUpRequest(string? Code);
}
