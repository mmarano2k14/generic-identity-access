using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the possible decisions returned by password login.</summary>
    public enum PasswordLoginDecision
    {
        /// <summary>Indicates the succeeded decision.</summary>
        Succeeded = 1,
        /// <summary>Indicates the invalid credentials decision.</summary>
        InvalidCredentials = 2,
        /// <summary>Indicates the client rejected decision.</summary>
        ClientRejected = 3,
        /// <summary>Indicates the redirect rejected decision.</summary>
        RedirectRejected = 4,
        /// <summary>Indicates the unavailable decision.</summary>
        Unavailable = 5
    }
}
