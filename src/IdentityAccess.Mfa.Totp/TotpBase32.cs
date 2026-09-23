using System.Text;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>RFC 4648 Base32 encoding used by authenticator-app provisioning.</summary>
    internal static class TotpBase32
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Encode(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return string.Empty;

            var output = new StringBuilder((data.Length * 8 + 4) / 5);
            var buffer = 0;
            var bitsInBuffer = 0;

            foreach (var value in data)
            {
                buffer = (buffer << 8) | value;
                bitsInBuffer += 8;

                while (bitsInBuffer >= 5)
                {
                    bitsInBuffer -= 5;
                    output.Append(Alphabet[(buffer >> bitsInBuffer) & 31]);
                }
            }

            if (bitsInBuffer > 0)
            {
                output.Append(Alphabet[(buffer << (5 - bitsInBuffer)) & 31]);
            }

            return output.ToString();
        }
    }
}
