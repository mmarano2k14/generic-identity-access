using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Resolves registered authentication-factor providers without hard-coding provider types.</summary>
    public interface IAuthenticationFactorProviderRegistry
    {
        /// <summary>Lists provider metadata in stable provider-key order.</summary>
        IReadOnlyList<AuthenticationFactorProviderDescriptor> List();

        /// <summary>Gets a registered provider by key, or null when it is not registered.</summary>
        IAuthenticationFactorProvider? Find(AuthenticationFactorProviderKey key);
    }
}
