using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Api
{
    internal sealed class ThrowingValidator : IOidcAccessTokenValidator
    {
        public OidcAccessTokenValidationResult Validate(string accessToken) =>
            throw new InvalidOperationException("test failure");
    }
}
