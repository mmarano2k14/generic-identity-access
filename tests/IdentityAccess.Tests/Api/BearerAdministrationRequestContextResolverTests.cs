using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityAccess.Tests.Api
{
    /// <summary>Verifies bearer access tokens are projected into trusted administration contexts.</summary>
    public sealed class BearerAdministrationRequestContextResolverTests
    {
        /// <summary>Verifies a validated bearer token becomes the administration request identity.</summary>
        [Fact]
        public async Task Valid_bearer_token_projects_trusted_context()
        {
            var token = ValidatedToken();
            var resolver = Resolver(
                OidcAccessTokenValidationResult.Success(token));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer encoded-token";

            var result = await resolver.ResolveAsync(
                httpContext,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAuthenticationDecision.Authenticated,
                result.Decision);
            Assert.NotNull(result.Context);
            Assert.Equal(token.Subject, result.Context!.Subject);
            Assert.Equal(token.SessionId, result.Context.SessionId);
            Assert.Equal(token.ClientId, result.Context.ClientId);
            Assert.Equal(token.Application, result.Context.Application);
            Assert.Equal(token.AuthenticationContextKey, result.Context.AuthenticationContextKey);
            Assert.Equal(token.ExpiresAt, result.Context.ExpiresAt);
        }

        /// <summary>Verifies invalid bearer credentials map to unauthenticated without leaking validator detail.</summary>
        [Fact]
        public async Task Invalid_bearer_token_is_unauthenticated()
        {
            var resolver = Resolver(
                OidcAccessTokenValidationResult.Invalid(
                    OidcAccessTokenValidationFailureCode.SignatureInvalid));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer invalid-token";

            var result = await resolver.ResolveAsync(
                httpContext,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAuthenticationDecision.Unauthenticated,
                result.Decision);
            Assert.Equal(
                AdministrationAuthenticationFailureCode.BearerTokenInvalid,
                result.FailureCode);
            Assert.Null(result.Context);
        }

        /// <summary>Verifies legacy session headers cannot be merged with Bearer authentication.</summary>
        [Fact]
        public async Task Bearer_and_local_session_headers_cannot_be_mixed()
        {
            var resolver = Resolver(
                OidcAccessTokenValidationResult.Success(ValidatedToken()));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer encoded-token";
            httpContext.Request.Headers[LocalSessionAdministrationRequestContextResolver.ClientHeaderName] = "web-client";

            var result = await resolver.ResolveAsync(
                httpContext,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAuthenticationDecision.Unauthenticated,
                result.Decision);
            Assert.Equal(
                AdministrationAuthenticationFailureCode.CredentialsMalformed,
                result.FailureCode);
        }


        /// <summary>Verifies current local-session ineligibility invalidates an otherwise valid Bearer token.</summary>
        [Fact]
        public async Task Invalid_current_session_state_is_unauthenticated()
        {
            var resolver = new BearerAdministrationRequestContextResolver(
                new FixedValidator(OidcAccessTokenValidationResult.Success(ValidatedToken())),
                new FixedSessionValidator(valid: false),
                NullLogger<BearerAdministrationRequestContextResolver>.Instance);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer encoded-token";

            var result = await resolver.ResolveAsync(
                httpContext,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAuthenticationDecision.Unauthenticated,
                result.Decision);
            Assert.Equal(
                AdministrationAuthenticationFailureCode.BearerTokenInvalid,
                result.FailureCode);
        }

        /// <summary>Verifies unexpected validator failures remain technical unavailability rather than denial.</summary>
        [Fact]
        public async Task Validator_exception_is_technical_unavailability()
        {
            var resolver = new BearerAdministrationRequestContextResolver(
                new ThrowingValidator(),
                new FixedSessionValidator(valid: true),
                NullLogger<BearerAdministrationRequestContextResolver>.Instance);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer encoded-token";

            var result = await resolver.ResolveAsync(
                httpContext,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAuthenticationDecision.Unavailable,
                result.Decision);
            Assert.Equal(
                AdministrationAuthenticationFailureCode.BearerValidationUnavailable,
                result.FailureCode);
        }

        private static BearerAdministrationRequestContextResolver Resolver(
            OidcAccessTokenValidationResult result) =>
            new(
                new FixedValidator(result),
                new FixedSessionValidator(valid: true),
                NullLogger<BearerAdministrationRequestContextResolver>.Instance);

        private static ValidatedOidcAccessToken ValidatedToken()
        {
            var issuedAt = DateTimeOffset.UtcNow;

            return new ValidatedOidcAccessToken(
                new SubjectReference(
                    Guid.Parse("40222222-2222-2222-2222-222222222222"),
                    Guid.Parse("40cccccc-cccc-cccc-cccc-cccccccccccc")),
                Guid.Parse("40dddddd-dddd-dddd-dddd-dddddddddddd"),
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                "openid",
                Guid.Parse("40eeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                issuedAt,
                issuedAt.AddMinutes(15));
        }

    }
}
