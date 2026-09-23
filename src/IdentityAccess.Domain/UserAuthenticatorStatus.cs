namespace IdentityAccess.Domain
{
    /// <summary>Defines the lifecycle state of one generic user authenticator.</summary>
    public enum UserAuthenticatorStatus
    {
        /// <summary>The provider enrollment has started but has not yet been confirmed.</summary>
        Pending = 1,
        /// <summary>The authenticator is confirmed and may be used by its provider.</summary>
        Active = 2,
        /// <summary>The authenticator has been revoked and cannot be used again.</summary>
        Revoked = 3
    }
}
