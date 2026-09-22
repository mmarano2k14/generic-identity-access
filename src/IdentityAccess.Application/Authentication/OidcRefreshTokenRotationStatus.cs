namespace IdentityAccess.Application.Authentication
{
    /// <summary>Defines the result of one atomic refresh-token rotation attempt.</summary>
    public enum OidcRefreshTokenRotationStatus
    {
        /// <summary>The presented token is unknown, expired, revoked, mismatched, or session-ineligible.</summary>
        Invalid = 1,

        /// <summary>The presented token was consumed and a replacement was persisted atomically.</summary>
        Rotated = 2,

        /// <summary>A previously consumed token was replayed and its entire family was revoked.</summary>
        ReuseDetected = 3
    }
}
