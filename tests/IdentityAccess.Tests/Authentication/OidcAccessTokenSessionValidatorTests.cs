using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>Verifies bearer access tokens remain tied to current local-session and user state.</summary>
    public sealed class OidcAccessTokenSessionValidatorTests
    {
        /// <summary>Verifies matching current session metadata preserves bearer eligibility.</summary>
        [Fact]
        public async Task Current_matching_session_is_valid()
        {
            var token = Token();
            var route = Route(token.Subject.IdentityScopeId, token.Application);
            var session = Session(token);
            var validator = new OidcAccessTokenSessionValidator(
                new FixedDirectoryLocator(route),
                new FixedSessionStore(session),
                TimeProvider.System);

            var valid = await validator.ValidateAsync(
                token,
                TestContext.Current.CancellationToken);

            Assert.True(valid);
        }

        /// <summary>Verifies a revoked, expired, or inactive-user session represented by no active row invalidates Bearer use.</summary>
        [Fact]
        public async Task Missing_current_session_state_invalidates_bearer_use()
        {
            var token = Token();
            var validator = new OidcAccessTokenSessionValidator(
                new FixedDirectoryLocator(Route(token.Subject.IdentityScopeId, token.Application)),
                new FixedSessionStore(null),
                TimeProvider.System);

            var valid = await validator.ValidateAsync(
                token,
                TestContext.Current.CancellationToken);

            Assert.False(valid);
        }

        /// <summary>Verifies a token cannot cross the identity-scope route selected by the trusted client context.</summary>
        [Fact]
        public async Task Identity_scope_route_mismatch_invalidates_bearer_use()
        {
            var token = Token();
            var validator = new OidcAccessTokenSessionValidator(
                new FixedDirectoryLocator(
                    Route(
                        Guid.Parse("40333333-3333-3333-3333-333333333333"),
                        token.Application)),
                new FixedSessionStore(Session(token)),
                TimeProvider.System);

            var valid = await validator.ValidateAsync(
                token,
                TestContext.Current.CancellationToken);

            Assert.False(valid);
        }

        private static ValidatedOidcAccessToken Token()
        {
            var issuedAt = DateTimeOffset.UtcNow;

            return new ValidatedOidcAccessToken(
                new SubjectReference(
                    Guid.Parse("40444444-4444-4444-4444-444444444444"),
                    Guid.Parse("40aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                Guid.Parse("40bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                "openid",
                Guid.Parse("40cccccc-cccc-cccc-cccc-cccccccccccc"),
                issuedAt,
                issuedAt.AddMinutes(15));
        }

        private static AuthenticationSession Session(
            ValidatedOidcAccessToken token) =>
            new(
                token.SessionId,
                token.Subject,
                token.ClientId,
                token.Application,
                token.AuthenticationContextKey,
                token.IssuedAt.AddMinutes(-1),
                token.ExpiresAt.AddMinutes(30));

        private static ResolvedDatabaseRoute Route(
            Guid identityScopeId,
            ApplicationKey application) =>
            new(
                new DatabaseRouteRequest(
                    application,
                    identityScopeId),
                "identity-a",
                new ConnectionSecretReference("env:IDENTITY_A"),
                1,
                1);

    }
}
