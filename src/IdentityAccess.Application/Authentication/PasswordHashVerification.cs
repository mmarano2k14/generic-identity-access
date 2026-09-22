using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines supported password hash verification values.</summary>
    public enum PasswordHashVerification
    {
        /// <summary>Represents the failed value.</summary>
        Failed = 0,
        /// <summary>Represents the success value.</summary>
        Success = 1,
        /// <summary>Represents the success rehash needed value.</summary>
        SuccessRehashNeeded = 2
    }
}
