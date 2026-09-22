using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Provides one deterministic registered client for OIDC protocol tests.</summary>
    internal sealed class OidcTestClientRegistry(
        AuthenticationClientRegistration client)
        : IAuthenticationClientRegistry
    {
        /// <inheritdoc />
        public bool TryGet(
            string clientId,
            out AuthenticationClientRegistration registration)
        {
            if (string.Equals(
                    clientId,
                    client.ClientId,
                    StringComparison.Ordinal))
            {
                registration = client;
                return true;
            }

            registration = null!;
            return false;
        }
    }
}
