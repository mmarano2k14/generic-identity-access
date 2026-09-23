using System.Security.Cryptography;
using System.Text;

namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Generates high-entropy human-enterable recovery codes and hashes canonical proofs.</summary>
    internal static class RecoveryCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string[] GenerateSet(int count)
        {
            if (count is < RecoveryCodeProviderOptions.MinimumCodeCount or > RecoveryCodeProviderOptions.MaximumCodeCount)
                throw new ArgumentOutOfRangeException(nameof(count));

            var codes = new HashSet<string>(StringComparer.Ordinal);
            while (codes.Count < count)
            {
                codes.Add(GenerateOne());
            }

            return codes.ToArray();
        }

        public static bool TryHash(string code, out byte[] hash)
        {
            hash = [];
            if (string.IsNullOrWhiteSpace(code)) return false;

            var canonical = new char[RecoveryCodeProviderOptions.CodeCharacterLength];
            try
            {
                var position = 0;
                foreach (var value in code.Trim())
                {
                    if (value == '-') continue;
                    if (position >= canonical.Length) return false;

                    var upper = char.ToUpperInvariant(value);
                    if (Alphabet.IndexOf(upper) < 0) return false;
                    canonical[position++] = upper;
                }

                if (position != canonical.Length) return false;

                var bytes = Encoding.ASCII.GetBytes(canonical);
                try
                {
                    hash = SHA256.HashData(bytes);
                    return true;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(bytes);
                }
            }
            finally
            {
                Array.Clear(canonical);
            }
        }

        private static string GenerateOne()
        {
            var canonical = new char[RecoveryCodeProviderOptions.CodeCharacterLength];
            try
            {
                for (var index = 0; index < canonical.Length; index++)
                {
                    canonical[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
                }

                return string.Create(
                    canonical.Length + 3,
                    canonical,
                    static (span, source) =>
                    {
                        var sourceIndex = 0;
                        var targetIndex = 0;
                        while (sourceIndex < source.Length)
                        {
                            if (sourceIndex > 0 && sourceIndex % 4 == 0)
                            {
                                span[targetIndex++] = '-';
                            }

                            span[targetIndex++] = source[sourceIndex++];
                        }
                    });
            }
            finally
            {
                Array.Clear(canonical);
            }
        }
    }
}
