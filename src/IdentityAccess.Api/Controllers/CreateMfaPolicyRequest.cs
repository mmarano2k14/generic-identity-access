using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Creates the initial generic MFA policy for an application.</summary>
    public sealed record CreateMfaPolicyRequest(
        MfaPolicyMode Mode,
        IReadOnlyList<string> AllowedProviders);
}
