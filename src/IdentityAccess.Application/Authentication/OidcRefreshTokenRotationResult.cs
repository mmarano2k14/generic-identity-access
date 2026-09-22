namespace IdentityAccess.Application.Authentication
{
    /// <summary>Represents the outcome of one atomic refresh-token rotation attempt.</summary>
    public sealed record OidcRefreshTokenRotationResult
    {
        /// <summary>Gets the typed rotation status.</summary>
        public OidcRefreshTokenRotationStatus Status { get; }

        /// <summary>Gets replacement grant metadata when rotation succeeded.</summary>
        public OidcRefreshTokenGrant? Grant { get; }

        private OidcRefreshTokenRotationResult(
            OidcRefreshTokenRotationStatus status,
            OidcRefreshTokenGrant? grant)
        {
            if (status == OidcRefreshTokenRotationStatus.Rotated && grant is null)
                throw new ArgumentException("Successful rotation requires a replacement grant.", nameof(grant));

            if (status != OidcRefreshTokenRotationStatus.Rotated && grant is not null)
                throw new ArgumentException("Failed rotation must not expose a grant.", nameof(grant));

            Status = status;
            Grant = grant;
        }

        /// <summary>Creates a successful rotation result.</summary>
        public static OidcRefreshTokenRotationResult Rotated(
            OidcRefreshTokenGrant grant)
        {
            ArgumentNullException.ThrowIfNull(grant);
            return new OidcRefreshTokenRotationResult(
                OidcRefreshTokenRotationStatus.Rotated,
                grant);
        }

        /// <summary>Creates an invalid-token rotation result.</summary>
        public static OidcRefreshTokenRotationResult Invalid() =>
            new(
                OidcRefreshTokenRotationStatus.Invalid,
                null);

        /// <summary>Creates a consumed-token replay result after family revocation.</summary>
        public static OidcRefreshTokenRotationResult ReuseDetected() =>
            new(
                OidcRefreshTokenRotationStatus.ReuseDetected,
                null);
    }
}
