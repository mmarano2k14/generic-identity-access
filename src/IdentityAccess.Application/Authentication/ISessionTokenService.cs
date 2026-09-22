

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the contract for session token service.</summary>
    public interface ISessionTokenService
    {
        /// <summary>Issues a cryptographically random opaque session token and its storage hash.</summary>
        IssuedSessionToken Issue();
        /// <summary>Computes the storage hash for an opaque session token.</summary>
        byte[] Hash(string token);
    }
}
