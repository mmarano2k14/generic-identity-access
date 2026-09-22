using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Api
{
    internal sealed class FixedSessionValidator(bool valid) : IOidcAccessTokenSessionValidator
    {
        public Task<bool> ValidateAsync(
            ValidatedOidcAccessToken token,
            CancellationToken cancellationToken) =>
            Task.FromResult(valid);
    }
}
