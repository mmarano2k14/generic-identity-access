using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Stable key for the WebAuthn authentication-factor provider.</summary>
    internal static class WebAuthnAuthenticationFactorProviderKey
    {
        internal const string Value = "webauthn";
        internal static AuthenticationFactorProviderKey Instance { get; } = new(Value);
    }
}
