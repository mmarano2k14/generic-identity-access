using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Canonical provider key shared by TOTP registration and persistence.</summary>
    internal static class TotpAuthenticationFactorProviderKey
    {
        public static AuthenticationFactorProviderKey Instance { get; } = new("totp");
        public static string Value => Instance.Value;
    }
}
