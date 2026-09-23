using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>Uses ASP.NET Core Data Protection with a provider-specific cryptographic purpose.</summary>
    internal sealed class TotpSecretProtector : ITotpSecretProtector
    {
        private readonly IDataProtector _protector;

        public TotpSecretProtector(IDataProtectionProvider dataProtectionProvider)
        {
            ArgumentNullException.ThrowIfNull(dataProtectionProvider);
            _protector = dataProtectionProvider.CreateProtector(
                "IdentityAccess.Mfa.Totp.Secret",
                "v1");
        }

        public byte[] Protect(ReadOnlySpan<byte> secret)
        {
            if (secret.IsEmpty) throw new ArgumentException("TOTP secret is required.", nameof(secret));

            var plaintext = secret.ToArray();
            try
            {
                return _protector.Protect(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }

        public byte[] Unprotect(byte[] protectedSecret)
        {
            ArgumentNullException.ThrowIfNull(protectedSecret);
            if (protectedSecret.Length == 0)
                throw new ArgumentException("Protected TOTP secret is required.", nameof(protectedSecret));
            return _protector.Unprotect(protectedSecret);
        }
    }
}
