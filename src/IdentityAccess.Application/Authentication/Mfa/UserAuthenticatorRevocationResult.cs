using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Result of one atomic generic-authenticator revocation attempt.</summary>
    public sealed record UserAuthenticatorRevocationResult(
        UserAuthenticatorRevocationDecision Decision,
        VersionedRecord<UserAuthenticator>? Record = null);
}
