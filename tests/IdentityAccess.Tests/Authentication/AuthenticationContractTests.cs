using System.Reflection;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{

    public sealed class AuthenticationContractTests
    {
        [Theory]
        [InlineData(typeof(ILocalAuthenticationService))]
        [InlineData(typeof(ICredentialAdministrationService))]
        [InlineData(typeof(IPasswordCredentialStore))]
        [InlineData(typeof(IAuthenticationSessionStore))]
        [InlineData(typeof(ISessionAdministrationService))]
        [InlineData(typeof(ICredentialMutationStore))]
        [InlineData(typeof(IOidcAuthorizationService))]
        [InlineData(typeof(IOidcAuthorizationCodeStore))]
        [InlineData(typeof(IOidcRefreshTokenStore))]
        [InlineData(typeof(IOidcAccessTokenSessionValidator))]
        public void Authentication_contracts_require_explicit_cancellation_tokens(Type contract)
        {
            foreach (var method in contract
                .GetMethods()
                .Where(method => !method.IsSpecialName))
            {
                var cancellation = method.GetParameters().LastOrDefault();
                Assert.NotNull(cancellation);
                Assert.Equal(typeof(CancellationToken), cancellation!.ParameterType);
                Assert.False(cancellation.HasDefaultValue);
                Assert.False(cancellation.IsOptional);
            }
        }

        [Fact]
        public void Login_identifier_normalization_is_deterministic_and_not_the_user_identity()
        {
            var identifier = new LoginIdentifier("  Example.User@Example.test  ");
            Assert.Equal("Example.User@Example.test", identifier.Value);
            Assert.Equal("EXAMPLE.USER@EXAMPLE.TEST", identifier.NormalizedValue);
        }

        [Fact]
        public void Password_credential_text_never_contains_the_hash()
        {
            var credential = new PasswordCredential(
                new SubjectReference(Guid.NewGuid(), Guid.NewGuid()),
                new LoginIdentifier("user@example.test"),
                "sensitive-password-hash");

            Assert.DoesNotContain("sensitive-password-hash", credential.ToString(), StringComparison.Ordinal);
            Assert.Contains("redacted", credential.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
