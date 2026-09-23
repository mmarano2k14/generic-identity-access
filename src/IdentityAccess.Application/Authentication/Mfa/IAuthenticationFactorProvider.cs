namespace IdentityAccess.Application.Authentication.Mfa
{
    /// <summary>
    /// Base contract for pluggable authentication-factor providers. Provider-specific enrollment
    /// and verification contracts are added by capability-specific interfaces rather than by the core.
    /// </summary>
    public interface IAuthenticationFactorProvider
    {
        /// <summary>Gets immutable provider metadata.</summary>
        AuthenticationFactorProviderDescriptor Descriptor { get; }
    }
}
