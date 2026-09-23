namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>Raised when a policy references a provider that is not registered in the current host.</summary>
    public sealed class AuthenticationFactorProviderNotRegisteredException : Exception
    {
        /// <summary>Initializes the exception without provider-sensitive state.</summary>
        public AuthenticationFactorProviderNotRegisteredException()
            : base("The requested authentication-factor provider is not registered on this host.")
        {
        }
    }
}
