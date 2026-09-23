using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Canonical provider key shared by recovery-code registration and persistence.</summary>
    internal static class RecoveryAuthenticationFactorProviderKey
    {
        public static AuthenticationFactorProviderKey Instance { get; } = new("recovery");
        public static string Value => Instance.Value;
    }
}
