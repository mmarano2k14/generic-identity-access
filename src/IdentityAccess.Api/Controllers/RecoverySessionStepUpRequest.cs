namespace IdentityAccess.Api.Controllers
{
    /// <summary>Contains one recovery-code proof for local-session step-up.</summary>
    public sealed record RecoverySessionStepUpRequest(string? Code);
}
