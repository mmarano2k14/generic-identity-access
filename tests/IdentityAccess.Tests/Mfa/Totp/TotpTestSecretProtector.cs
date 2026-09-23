using IdentityAccess.Mfa.Totp;

namespace IdentityAccess.Tests.Mfa.Totp
{
    internal sealed class TotpTestSecretProtector : ITotpSecretProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> secret) => secret.ToArray();

        public byte[] Unprotect(byte[] protectedSecret) => protectedSecret.ToArray();
    }
}
