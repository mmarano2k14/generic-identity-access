using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authentication
{
    /// <summary>
    /// Verifies strict Authorization Code + PKCE and refresh-token rotation without HTTP transport.
    /// </summary>
    public sealed class OidcAuthorizationServiceTests
    {
        private static readonly Guid ScopeId =
            Guid.Parse("37111111-1111-1111-1111-111111111111");

        private static readonly Guid UserId =
            Guid.Parse("37aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly Guid SessionId =
            Guid.Parse("37bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private const string RedirectUri =
            "https://client.example.test/callback";

        private const string CodeVerifier =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._~abc";

        /// <summary>Verifies a valid request persists only code metadata/hash and returns a code.</summary>
        [Fact]
        public async Task Valid_authorization_request_issues_one_time_code()
        {
            var fixture = Create();

            var result = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(RedirectUri, result.RedirectUri);
            Assert.Equal("state-12345678", result.State);
            Assert.Equal(fixture.CodeService.Code, result.Code);

            var grant = Assert.IsType<OidcAuthorizationCodeGrant>(
                fixture.Store.Grant);

            Assert.Equal(UserId, grant.Subject.UserId);
            Assert.Equal(SessionId, grant.SessionId);
            Assert.Equal("openid", grant.Scope);
            Assert.Equal("nonce-12345678", grant.Nonce);
            Assert.Equal(32, Assert.IsType<byte[]>(fixture.Store.CodeHash).Length);
        }

        /// <summary>Verifies a validated redirect receives login_required when no local session exists.</summary>
        [Fact]
        public async Task Missing_local_session_returns_login_required_on_valid_redirect()
        {
            var fixture = Create();

            var result = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                session: null,
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcAuthorizationFailureCode.LoginRequired,
                result.FailureCode);
            Assert.Equal(RedirectUri, result.RedirectUri);
            Assert.Equal("state-12345678", result.State);
        }

        /// <summary>Verifies an unregistered redirect is never used as an error redirect target.</summary>
        [Fact]
        public async Task Unregistered_redirect_is_rejected_without_redirect()
        {
            var fixture = Create();

            var request = AuthorizationRequest(
                fixture.CodeService.ComputeS256Challenge(
                    CodeVerifier)) with
            {
                RedirectUri = "https://attacker.example.test/callback"
            };

            var result = await fixture.Service.AuthorizeAsync(
                request,
                Session(),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcAuthorizationFailureCode.RedirectUriRejected,
                result.FailureCode);
            Assert.Null(result.RedirectUri);
        }

        /// <summary>
        /// Verifies a session revoked between initial validation and code persistence cannot receive
        /// an authorization code.
        /// </summary>
        [Fact]
        public async Task Concurrent_session_revocation_prevents_code_issue()
        {
            var fixture = Create();
            fixture.Store.AllowCreate = false;

            var result = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcAuthorizationFailureCode.LoginRequired,
                result.FailureCode);
            Assert.Null(fixture.Store.Grant);
        }

        /// <summary>Verifies the current release refuses unsupported OIDC scopes.</summary>
        [Fact]
        public async Task Unsupported_scope_is_rejected()
        {
            var fixture = Create();

            var request = AuthorizationRequest(
                fixture.CodeService.ComputeS256Challenge(
                    CodeVerifier)) with
            {
                Scope = "openid profile"
            };

            var result = await fixture.Service.AuthorizeAsync(
                request,
                Session(),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcAuthorizationFailureCode.InvalidScope,
                result.FailureCode);
        }

        /// <summary>Verifies a valid PKCE exchange succeeds once and cannot be replayed.</summary>
        [Fact]
        public async Task Authorization_code_exchange_is_single_use()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            Assert.True(authorization.Succeeded);

            var request = new OidcTokenRequest(
                "web-client",
                "authorization_code",
                authorization.Code,
                RedirectUri,
                CodeVerifier);

            var first = await fixture.Service.ExchangeCodeAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.True(first.Succeeded);
            Assert.Equal("access-token", first.AccessToken);
            Assert.Equal("id-token", first.IdToken);
            Assert.Equal(new string('A', 43), first.RefreshToken);
            Assert.Equal("openid", first.Scope);

            var replay = await fixture.Service.ExchangeCodeAsync(
                request,
                TestContext.Current.CancellationToken);

            Assert.False(replay.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                replay.FailureCode);
        }


        /// <summary>Verifies authorization-code exchange creates the initial refresh-token family.</summary>
        [Fact]
        public async Task Authorization_code_exchange_creates_initial_refresh_family()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            var result = await fixture.Service.ExchangeCodeAsync(
                new OidcTokenRequest(
                    "web-client",
                    "authorization_code",
                    authorization.Code,
                    RedirectUri,
                    CodeVerifier),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);

            var refreshGrant =
                Assert.IsType<OidcRefreshTokenGrant>(
                    fixture.RefreshTokenStore.CurrentGrant);

            Assert.Equal(0L, refreshGrant.SequenceNumber);
            Assert.Null(refreshGrant.ParentTokenId);
            Assert.Equal(SessionId, refreshGrant.SessionId);
        }

        /// <summary>Verifies a refresh token rotates once and does not issue a new ID token.</summary>
        [Fact]
        public async Task Refresh_token_rotates_and_issues_access_token_only()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            var initial = await fixture.Service.ExchangeCodeAsync(
                new OidcTokenRequest(
                    "web-client",
                    "authorization_code",
                    authorization.Code,
                    RedirectUri,
                    CodeVerifier),
                TestContext.Current.CancellationToken);

            var initialGrant =
                Assert.IsType<OidcRefreshTokenGrant>(
                    fixture.RefreshTokenStore.CurrentGrant);

            var refreshed = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "web-client",
                    initial.RefreshToken),
                TestContext.Current.CancellationToken);

            Assert.True(refreshed.Succeeded);
            Assert.Equal("refresh-access-token", refreshed.AccessToken);
            Assert.Null(refreshed.IdToken);
            Assert.Equal(new string('B', 43), refreshed.RefreshToken);
            Assert.Equal("openid", refreshed.Scope);

            var replacement =
                Assert.IsType<OidcRefreshTokenGrant>(
                    fixture.RefreshTokenStore.CurrentGrant);

            Assert.Equal(initialGrant.FamilyId, replacement.FamilyId);
            Assert.Equal(initialGrant.TokenId, replacement.ParentTokenId);
            Assert.Equal(1L, replacement.SequenceNumber);
            Assert.Equal(initialGrant.AuthenticatedAt, replacement.AuthenticatedAt);
            Assert.Equal(initialGrant.ExpiresAt, replacement.ExpiresAt);
        }

        /// <summary>Verifies replay of a consumed refresh token revokes its whole family.</summary>
        [Fact]
        public async Task Consumed_refresh_token_replay_revokes_family()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            var initial = await fixture.Service.ExchangeCodeAsync(
                new OidcTokenRequest(
                    "web-client",
                    "authorization_code",
                    authorization.Code,
                    RedirectUri,
                    CodeVerifier),
                TestContext.Current.CancellationToken);

            var rotated = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "web-client",
                    initial.RefreshToken),
                TestContext.Current.CancellationToken);

            Assert.True(rotated.Succeeded);

            var replay = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "web-client",
                    initial.RefreshToken),
                TestContext.Current.CancellationToken);

            Assert.False(replay.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                replay.FailureCode);
            Assert.True(fixture.RefreshTokenStore.FamilyRevoked);

            var afterRevocation = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "web-client",
                    rotated.RefreshToken),
                TestContext.Current.CancellationToken);

            Assert.False(afterRevocation.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                afterRevocation.FailureCode);
        }

        /// <summary>Verifies refresh fails when the linked local session or user is no longer eligible.</summary>
        [Fact]
        public async Task Ineligible_local_session_blocks_refresh()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            var initial = await fixture.Service.ExchangeCodeAsync(
                new OidcTokenRequest(
                    "web-client",
                    "authorization_code",
                    authorization.Code,
                    RedirectUri,
                    CodeVerifier),
                TestContext.Current.CancellationToken);

            fixture.RefreshTokenStore.RotationEligible = false;

            var result = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "web-client",
                    initial.RefreshToken),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                result.FailureCode);
        }

        /// <summary>Verifies family creation rechecks the local session before any token response is returned.</summary>
        [Fact]
        public async Task Refresh_family_creation_rechecks_active_session_before_token_response()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            fixture.RefreshTokenStore.AllowCreate = false;

            var result = await fixture.Service.ExchangeCodeAsync(
                new OidcTokenRequest(
                    "web-client",
                    "authorization_code",
                    authorization.Code,
                    RedirectUri,
                    CodeVerifier),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                result.FailureCode);
            Assert.Null(result.AccessToken);
            Assert.Null(result.IdToken);
            Assert.Null(result.RefreshToken);
            Assert.Null(fixture.RefreshTokenStore.CurrentGrant);
        }

        /// <summary>Verifies unknown opaque refresh tokens fail without exposing family state.</summary>
        [Fact]
        public async Task Unknown_refresh_token_is_invalid_grant()
        {
            var fixture = Create();

            var result = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "web-client",
                    new string('Z', 43)),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                result.FailureCode);
        }

        /// <summary>Verifies an unregistered client cannot present a refresh token.</summary>
        [Fact]
        public async Task Unknown_client_cannot_use_refresh_token()
        {
            var fixture = Create();

            var result = await fixture.Service.RefreshAsync(
                new OidcRefreshTokenRequest(
                    "other-client",
                    new string('Z', 43)),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidClient,
                result.FailureCode);
        }

        /// <summary>Verifies a wrong PKCE verifier cannot consume the authorization code.</summary>
        [Fact]
        public async Task Wrong_pkce_verifier_is_invalid_grant()
        {
            var fixture = Create();

            var authorization = await fixture.Service.AuthorizeAsync(
                AuthorizationRequest(
                    fixture.CodeService.ComputeS256Challenge(
                        CodeVerifier)),
                Session(),
                TestContext.Current.CancellationToken);

            var result = await fixture.Service.ExchangeCodeAsync(
                new OidcTokenRequest(
                    "web-client",
                    "authorization_code",
                    authorization.Code,
                    RedirectUri,
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~xyz"),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(
                OidcTokenFailureCode.InvalidGrant,
                result.FailureCode);
        }

        private static OidcAuthorizationRequest AuthorizationRequest(
            string codeChallenge) =>
            new(
                "web-client",
                RedirectUri,
                "code",
                "openid",
                "state-12345678",
                "nonce-12345678",
                codeChallenge,
                "S256");

        private static AuthenticatedSessionContext Session()
        {
            var authenticatedAt =
                DateTimeOffset.UtcNow.AddMinutes(-1);

            return new AuthenticatedSessionContext(
                new SubjectReference(
                    ScopeId,
                    UserId),
                SessionId,
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                authenticatedAt,
                authenticatedAt.AddHours(1));
        }

        private static OidcAuthorizationServiceFixture Create()
        {
            var client = new AuthenticationClientRegistration(
                "web-client",
                new ApplicationKey("app-a"),
                "app-a-primary",
                [RedirectUri],
                oidcEnabled: true,
                allowedOidcScopes: ["openid"]);

            var codeService =
                new OidcTestCodeService();

            var store =
                new OidcTestAuthorizationCodeStore();

            var refreshTokenStore =
                new OidcTestRefreshTokenStore();

            var refreshTokenService =
                new OidcTestRefreshTokenService();

            var service =
                new OidcAuthorizationService(
                    new OidcTestClientRegistry(client),
                    new OidcTestDirectoryLocator(
                        ScopeId,
                        new ApplicationKey("app-a"),
                        "app-a-primary"),
                    store,
                    refreshTokenStore,
                    codeService,
                    refreshTokenService,
                    new OidcTestTokenIssuer(),
                    new OidcOptions
                    {
                        Issuer = "https://identity.example.test",
                        AccessTokenAudience = "identity-api"
                    },
                    TimeProvider.System,
                    new OidcTestSecurityAuditWriter());

            return new OidcAuthorizationServiceFixture(
                service,
                store,
                codeService,
                refreshTokenStore,
                refreshTokenService);
        }
    }
}
