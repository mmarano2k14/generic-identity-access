using System.Text;
using IdentityAccess.Mfa.Totp;

namespace IdentityAccess.Tests.Mfa.Totp
{
    public sealed class TotpCodeGeneratorTests
    {
        [Theory]
        [InlineData(59L, "94287082")]
        [InlineData(1111111109L, "07081804")]
        [InlineData(1111111111L, "14050471")]
        [InlineData(1234567890L, "89005924")]
        [InlineData(2000000000L, "69279037")]
        [InlineData(20000000000L, "65353130")]
        public void Rfc6238_sha1_vectors_match(long unixTimeSeconds, string expected)
        {
            var secret = Encoding.ASCII.GetBytes("12345678901234567890");

            var actual = TotpCodeGenerator.Generate(
                secret,
                unixTimeSeconds,
                digits: 8,
                periodSeconds: 30);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Default_profile_generates_six_digits()
        {
            var secret = Encoding.ASCII.GetBytes("12345678901234567890");
            var code = TotpCodeGenerator.Generate(secret, 59, digits: 6, periodSeconds: 30);

            Assert.Equal(6, code.Length);
            Assert.All(code, character => Assert.InRange(character, '0', '9'));
        }
    }
}
