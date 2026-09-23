using System.Text;
using IdentityAccess.Mfa.Totp;

namespace IdentityAccess.Tests.Mfa.Totp
{
    public sealed class TotpBase32Tests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("f", "MY")]
        [InlineData("fo", "MZXQ")]
        [InlineData("foo", "MZXW6")]
        [InlineData("foob", "MZXW6YQ")]
        [InlineData("fooba", "MZXW6YTB")]
        [InlineData("foobar", "MZXW6YTBOI")]
        public void Rfc4648_unpadded_vectors_match(string input, string expected)
        {
            Assert.Equal(expected, TotpBase32.Encode(Encoding.ASCII.GetBytes(input)));
        }
    }
}
