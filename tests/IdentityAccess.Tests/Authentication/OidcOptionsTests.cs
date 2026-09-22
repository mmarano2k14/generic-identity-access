using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies trusted OIDC provider configuration boundaries.</summary>
    public sealed class OidcOptionsTests
    {
        /// <summary>Verifies HTTPS issuer configuration and loopback HTTP development issuers.</summary>
        [Theory]
        [InlineData("https://identity.example.test")]
        [InlineData("http://127.0.0.1:5080")]
        [InlineData("http://localhost:5080")]
        public void Valid_issuers_are_accepted(string issuer)
        {
            var options = new OidcOptions
            {
                Issuer = issuer
            };

            Assert.Same(
                options,
                options.Validate());
        }

        /// <summary>Verifies insecure non-loopback and ambiguous issuer URIs are rejected.</summary>
        [Theory]
        [InlineData("http://identity.example.test")]
        [InlineData("https://user@identity.example.test")]
        [InlineData("https://identity.example.test?x=1")]
        [InlineData("https://identity.example.test#fragment")]
        [InlineData("https://identity.example.test/issuer")]
        public void Unsafe_issuers_are_rejected(string issuer)
        {
            Assert.Throws<ArgumentException>(() =>
                new OidcOptions
                {
                    Issuer = issuer
                }.Validate());
        }

        /// <summary>Verifies refresh-token families use a finite validated absolute lifetime.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(366)]
        public void Unsafe_refresh_token_lifetimes_are_rejected(int days)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OidcOptions
                {
                    Issuer = "https://identity.example.test",
                    RefreshTokenLifetimeDays = days
                }.Validate());
        }
    }
}
