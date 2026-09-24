using IdentityAccess.Application.Security;
using IdentityAccess.Domain;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    public sealed class WebAuthnAuthenticationServiceTests
    {
        private static readonly Guid ScopeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid AuthenticatorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly ApplicationKey Application = new("admin-web");

        [Fact]
        public async Task Valid_assertion_updates_counter_consumes_challenge_and_audits_success()
        {
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T01:00:00Z"));
            var store = new WebAuthnTestStore();
            var audit = new WebAuthnTestSecurityAuditWriter();
            using var credential = WebAuthnTestCredential.Create(ScopeId, UserId);
            store.SeedActiveCredential(ScopeId, UserId, AuthenticatorId, credential.Material, time.GetUtcNow());
            var service = CreateService(store, audit, time);

            var options = await service.BeginAuthenticationAsync(ScopeId, Application, UserId, CancellationToken.None);
            Assert.Equal("example.test", options.RelyingPartyId);
            Assert.Equal("required", options.UserVerification);
            Assert.Contains(WebAuthnBase64Url.Encode(credential.Material.CredentialId), options.AllowCredentialIds);

            var response = credential.CreateResponse(options, "https://login.example.test", signCount: 1);
            var result = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                options.ChallengeId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnAuthenticationResult.Succeeded, result);
            Assert.Equal(1, store.CredentialRecord!.SignCount);
            Assert.Equal(time.GetUtcNow(), store.LastUsedAt);
            Assert.Contains(audit.Events, value =>
                value.EventType == SecurityAuditEventType.AuthenticationFactorVerificationSucceeded &&
                value.Outcome == SecurityAuditOutcome.Succeeded);

            var replay = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                options.ChallengeId,
                response,
                CancellationToken.None);
            Assert.Equal(WebAuthnAuthenticationResult.AlreadyUsed, replay);
        }

        [Fact]
        public async Task Invalid_origin_is_rejected_before_signature_acceptance()
        {
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T01:00:00Z"));
            var store = new WebAuthnTestStore();
            using var credential = WebAuthnTestCredential.Create(ScopeId, UserId);
            store.SeedActiveCredential(ScopeId, UserId, AuthenticatorId, credential.Material, time.GetUtcNow());
            var service = CreateService(store, new WebAuthnTestSecurityAuditWriter(), time);

            var options = await service.BeginAuthenticationAsync(ScopeId, Application, UserId, CancellationToken.None);
            var response = credential.CreateResponse(options, "https://evil.example.test", signCount: 1);

            var result = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                options.ChallengeId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnAuthenticationResult.InvalidClientData, result);
            Assert.Equal(0, store.CredentialRecord!.SignCount);
        }

        [Fact]
        public async Task Invalid_signature_is_rejected()
        {
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T01:00:00Z"));
            var store = new WebAuthnTestStore();
            using var credential = WebAuthnTestCredential.Create(ScopeId, UserId);
            store.SeedActiveCredential(ScopeId, UserId, AuthenticatorId, credential.Material, time.GetUtcNow());
            var service = CreateService(store, new WebAuthnTestSecurityAuditWriter(), time);

            var options = await service.BeginAuthenticationAsync(ScopeId, Application, UserId, CancellationToken.None);
            var response = credential.CreateResponse(
                options,
                "https://login.example.test",
                signCount: 1,
                tamperSignature: true);

            var result = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                options.ChallengeId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnAuthenticationResult.InvalidAssertion, result);
        }

        [Fact]
        public async Task Non_increasing_nonzero_signature_counter_is_rejected_as_replay()
        {
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T01:00:00Z"));
            var store = new WebAuthnTestStore();
            using var credential = WebAuthnTestCredential.Create(ScopeId, UserId, signCount: 4);
            store.SeedActiveCredential(ScopeId, UserId, AuthenticatorId, credential.Material, time.GetUtcNow());
            var audit = new WebAuthnTestSecurityAuditWriter();
            var service = CreateService(store, audit, time);

            var options = await service.BeginAuthenticationAsync(ScopeId, Application, UserId, CancellationToken.None);
            var response = credential.CreateResponse(options, "https://login.example.test", signCount: 4);

            var result = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                options.ChallengeId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnAuthenticationResult.ReplayDetected, result);
            Assert.Contains(audit.Events, value =>
                value.EventType == SecurityAuditEventType.AuthenticationFactorVerificationFailed &&
                value.ReasonCode == SecurityAuditReasonCode.AuthenticationFactorReplayDetected);
        }

        [Fact]
        public async Task Zero_signature_counter_profile_remains_usable_for_counterless_authenticators()
        {
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T01:00:00Z"));
            var store = new WebAuthnTestStore();
            using var credential = WebAuthnTestCredential.Create(ScopeId, UserId, signCount: 0);
            store.SeedActiveCredential(ScopeId, UserId, AuthenticatorId, credential.Material, time.GetUtcNow());
            var service = CreateService(store, new WebAuthnTestSecurityAuditWriter(), time);

            var first = await service.BeginAuthenticationAsync(ScopeId, Application, UserId, CancellationToken.None);
            var firstResult = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                first.ChallengeId,
                credential.CreateResponse(first, "https://login.example.test", signCount: 0),
                CancellationToken.None);

            var second = await service.BeginAuthenticationAsync(ScopeId, Application, UserId, CancellationToken.None);
            var secondResult = await service.CompleteAuthenticationAsync(
                ScopeId,
                Application,
                UserId,
                second.ChallengeId,
                credential.CreateResponse(second, "https://login.example.test", signCount: 0),
                CancellationToken.None);

            Assert.Equal(WebAuthnAuthenticationResult.Succeeded, firstResult);
            Assert.Equal(WebAuthnAuthenticationResult.Succeeded, secondResult);
        }

        private static WebAuthnAuthenticationService CreateService(
            WebAuthnTestStore store,
            WebAuthnTestSecurityAuditWriter audit,
            TimeProvider timeProvider) =>
            new(
                new WebAuthnTestRouteResolver(),
                store,
                audit,
                new IdentityAccess.Tests.Mfa.AllowingMfaProviderPolicyGuard(),
                timeProvider,
                new WebAuthnProviderOptions(
                    "example.test",
                    "Example Identity",
                    ["https://login.example.test"],
                    WebAuthnProviderOptions.DefaultChallengeLifetimeSeconds));
    }
}
