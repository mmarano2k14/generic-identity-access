using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Api
{
    internal sealed class FixedValidator(
        OidcAccessTokenValidationResult result)
        : IOidcAccessTokenValidator
    {
        public OidcAccessTokenValidationResult Validate(string accessToken) =>
            result;
    }
}
