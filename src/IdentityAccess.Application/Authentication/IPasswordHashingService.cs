using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the contract for password hashing service.</summary>
    public interface IPasswordHashingService
    {
        /// <summary>Hashes a password for the supplied subject using the configured password hasher.</summary>
        string Hash(SubjectReference subject, string password);
        /// <summary>Verifies a supplied password against the stored password hash.</summary>
        PasswordHashVerification Verify(SubjectReference subject, string passwordHash, string providedPassword);
        /// <summary>Performs password-hash work for an unknown credential to reduce account-enumeration timing differences.</summary>
        void ConsumeUnknownCredential(string providedPassword);
    }
}
