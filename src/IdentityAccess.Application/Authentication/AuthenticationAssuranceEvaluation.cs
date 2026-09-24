using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{
    /// <summary>Describes the effective MFA requirement and whether a session currently satisfies it.</summary>
    public sealed record AuthenticationAssuranceEvaluation(
        MfaPolicyMode? PolicyMode,
        AuthenticationAssurance Assurance,
        bool Satisfied,
        bool Fresh);
}
