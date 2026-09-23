using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IdentityAccess.Mfa.Totp
{
    /// <summary>RFC 6238 code generation and fixed-time proof validation.</summary>
    internal static class TotpCodeGenerator
    {
        public static string Generate(
            ReadOnlySpan<byte> secret,
            long unixTimeSeconds,
            int digits,
            int periodSeconds)
        {
            if (secret.IsEmpty) throw new ArgumentException("TOTP secret is required.", nameof(secret));
            if (unixTimeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(unixTimeSeconds));
            if (digits is < 6 or > 8) throw new ArgumentOutOfRangeException(nameof(digits));
            if (periodSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(periodSeconds));

            var timeStep = unixTimeSeconds / periodSeconds;
            Span<byte> counter = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counter, timeStep);

            Span<byte> hash = stackalloc byte[20];
            HMACSHA1.HashData(secret, counter, hash);

            var offset = hash[^1] & 0x0f;
            var binary =
                ((hash[offset] & 0x7f) << 24) |
                (hash[offset + 1] << 16) |
                (hash[offset + 2] << 8) |
                hash[offset + 3];

            var modulus = digits switch
            {
                6 => 1_000_000,
                7 => 10_000_000,
                8 => 100_000_000,
                _ => throw new ArgumentOutOfRangeException(nameof(digits))
            };

            return (binary % modulus).ToString($"D{digits}", CultureInfo.InvariantCulture);
        }

        public static bool TryValidate(
            ReadOnlySpan<byte> secret,
            string code,
            long unixTimeSeconds,
            int digits,
            int periodSeconds,
            int allowedClockSkewSteps,
            out long acceptedTimeStep)
        {
            acceptedTimeStep = -1;
            if (!IsCanonicalCode(code, digits)) return false;
            if (allowedClockSkewSteps is < 0 or > 2)
                throw new ArgumentOutOfRangeException(nameof(allowedClockSkewSteps));

            var currentStep = unixTimeSeconds / periodSeconds;
            if (Matches(secret, code, currentStep, digits, periodSeconds))
            {
                acceptedTimeStep = currentStep;
                return true;
            }

            for (var distance = 1; distance <= allowedClockSkewSteps; distance++)
            {
                var previous = currentStep - distance;
                if (previous >= 0 && Matches(secret, code, previous, digits, periodSeconds))
                {
                    acceptedTimeStep = previous;
                    return true;
                }

                var next = currentStep + distance;
                if (Matches(secret, code, next, digits, periodSeconds))
                {
                    acceptedTimeStep = next;
                    return true;
                }
            }

            return false;
        }

        private static bool Matches(
            ReadOnlySpan<byte> secret,
            string code,
            long timeStep,
            int digits,
            int periodSeconds)
        {
            var expected = Generate(secret, checked(timeStep * periodSeconds), digits, periodSeconds);
            Span<byte> expectedBytes = stackalloc byte[8];
            Span<byte> actualBytes = stackalloc byte[8];
            var expectedLength = Encoding.ASCII.GetBytes(expected, expectedBytes);
            var actualLength = Encoding.ASCII.GetBytes(code, actualBytes);

            return expectedLength == actualLength &&
                   CryptographicOperations.FixedTimeEquals(
                       expectedBytes[..expectedLength],
                       actualBytes[..actualLength]);
        }

        private static bool IsCanonicalCode(string code, int digits)
        {
            if (code is null || code.Length != digits) return false;
            foreach (var character in code)
            {
                if (character is < '0' or > '9') return false;
            }

            return true;
        }
    }
}
