namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Defines the result of one atomic generic-authenticator revocation attempt.</summary>
    public enum UserAuthenticatorRevocationDecision
    {
        /// <summary>The authenticator was revoked or was already revoked at the requested version.</summary>
        Succeeded = 1,
        /// <summary>The authenticator does not exist for the requested user and scope.</summary>
        NotFound = 2,
        /// <summary>The optimistic-concurrency version no longer matches.</summary>
        VersionConflict = 3,
        /// <summary>Revocation would remove the final active verification factor required by policy.</summary>
        WouldViolateRequiredMfa = 4
    }
}
