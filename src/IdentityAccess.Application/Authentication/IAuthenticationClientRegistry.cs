

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines the contract for authentication client registry.</summary>
    public interface IAuthenticationClientRegistry
    {
        /// <summary>Attempts to resolve a registered authentication client by its stable client identifier.</summary>
        bool TryGet(string clientId, out AuthenticationClientRegistration registration);
    }
}
