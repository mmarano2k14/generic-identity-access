using IdentityAccess.Mfa.Recovery;

namespace IdentityAccess.Tests.Mfa.Recovery
{
    public sealed class RecoveryCodeGeneratorTests
    {
        [Fact]
        public void Generated_set_is_unique_and_uses_human_readable_groups()
        {
            var codes = RecoveryCodeGenerator.GenerateSet(10);

            Assert.Equal(10, codes.Length);
            Assert.Equal(10, codes.Distinct(StringComparer.Ordinal).Count());
            Assert.All(codes, code => Assert.Matches("^[A-HJ-NP-Z2-9]{4}(-[A-HJ-NP-Z2-9]{4}){3}$", code));
        }

        [Fact]
        public void Hashing_is_case_insensitive_and_hyphen_tolerant()
        {
            const string formatted = "ABCD-EFGH-JKLM-NPQR";
            const string compactLower = "abcdefghjklmnpqr";

            Assert.True(RecoveryCodeGenerator.TryHash(formatted, out var first));
            Assert.True(RecoveryCodeGenerator.TryHash(compactLower, out var second));
            Assert.Equal(first, second);
            Assert.Equal(32, first.Length);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABCD-EFGH")]
        [InlineData("ABCD-EFGH-JKLM-NPQ1")]
        public void Invalid_code_shapes_are_rejected(string value)
        {
            Assert.False(RecoveryCodeGenerator.TryHash(value, out var hash));
            Assert.Empty(hash);
        }
    }
}
