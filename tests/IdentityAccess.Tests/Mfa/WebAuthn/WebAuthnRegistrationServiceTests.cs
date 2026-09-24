using IdentityAccess.Application.Security;
using IdentityAccess.Domain;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    public sealed class WebAuthnRegistrationServiceTests
    {
        [Fact]
        public async Task Valid_none_attestation_registration_activates_public_credential()
        {
            var scopeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var application = new ApplicationKey("admin-web");
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T00:00:00Z"));
            var store = new WebAuthnTestStore();
            var audit = new WebAuthnTestSecurityAuditWriter();
            var service = CreateService(store, audit, time);

            var options = await service.BeginRegistrationAsync(
                scopeId,
                application,
                userId,
                "Laptop passkey",
                "user@example.test",
                "Example User",
                CancellationToken.None);

            Assert.Equal(UserAuthenticatorStatus.Pending, store.Status);
            Assert.Equal("none", options.Attestation);
            Assert.Equal("required", options.ResidentKey);
            Assert.Equal("required", options.UserVerification);
            Assert.Equal([WebAuthnProviderOptions.CoseAlgorithmEs256], options.PublicKeyCredentialAlgorithms);
            Assert.True(WebAuthnBase64Url.TryDecode(options.Challenge, 64, out var challenge));
            Assert.Equal(WebAuthnProviderOptions.ChallengeLengthBytes, challenge.Length);
            Assert.True(WebAuthnBase64Url.TryDecode(options.UserId, 64, out var userHandle));
            Assert.Equal(WebAuthnProviderOptions.UserHandleLengthBytes, userHandle.Length);

            var response = WebAuthnTestResponseFactory.Create(options, "https://login.example.test");
            var result = await service.CompleteRegistrationAsync(
                scopeId,
                application,
                userId,
                options.AuthenticatorId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnRegistrationResult.Registered, result);
            Assert.Equal(UserAuthenticatorStatus.Active, store.Status);
            Assert.NotNull(store.Credential);
            Assert.Equal(WebAuthnProviderOptions.CoseAlgorithmEs256, store.Credential!.CoseAlgorithm);
            Assert.True(store.Credential.BackupEligible);
            Assert.True(store.Credential.BackupState);
            Assert.Contains(audit.Events, item => item.EventType == SecurityAuditEventType.UserAuthenticatorEnrollmentConfirmed);
        }

        [Fact]
        public async Task Unexpected_origin_is_rejected_without_activating_authenticator()
        {
            var scopeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var application = new ApplicationKey("admin-web");
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T00:00:00Z"));
            var store = new WebAuthnTestStore();
            var service = CreateService(store, new WebAuthnTestSecurityAuditWriter(), time);

            var options = await service.BeginRegistrationAsync(
                scopeId,
                application,
                userId,
                "Laptop passkey",
                "user@example.test",
                "Example User",
                CancellationToken.None);
            var response = WebAuthnTestResponseFactory.Create(options, "https://other.example.test");

            var result = await service.CompleteRegistrationAsync(
                scopeId,
                application,
                userId,
                options.AuthenticatorId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnRegistrationResult.InvalidClientData, result);
            Assert.Equal(UserAuthenticatorStatus.Pending, store.Status);
            Assert.Null(store.Credential);
        }

        [Fact]
        public async Task Consumed_registration_challenge_cannot_be_replayed()
        {
            var scopeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            var application = new ApplicationKey("admin-web");
            var time = new WebAuthnTestTimeProvider(DateTimeOffset.Parse("2026-09-24T00:00:00Z"));
            var store = new WebAuthnTestStore();
            var service = CreateService(store, new WebAuthnTestSecurityAuditWriter(), time);

            var options = await service.BeginRegistrationAsync(
                scopeId,
                application,
                userId,
                "Laptop passkey",
                "user@example.test",
                "Example User",
                CancellationToken.None);
            var response = WebAuthnTestResponseFactory.Create(options, "https://login.example.test");

            var first = await service.CompleteRegistrationAsync(
                scopeId,
                application,
                userId,
                options.AuthenticatorId,
                response,
                CancellationToken.None);
            var replay = await service.CompleteRegistrationAsync(
                scopeId,
                application,
                userId,
                options.AuthenticatorId,
                response,
                CancellationToken.None);

            Assert.Equal(WebAuthnRegistrationResult.Registered, first);
            Assert.Equal(WebAuthnRegistrationResult.AlreadyUsed, replay);
        }

        private static WebAuthnRegistrationService CreateService(
            WebAuthnTestStore store,
            WebAuthnTestSecurityAuditWriter audit,
            WebAuthnTestTimeProvider time) =>
            new(
                new WebAuthnTestRouteResolver(),
                store,
                audit,
                new IdentityAccess.Tests.Mfa.AllowingMfaProviderPolicyGuard(),
                time,
                new WebAuthnProviderOptions(
                    "example.test",
                    "Generic Identity Access",
                    ["https://login.example.test"],
                    WebAuthnProviderOptions.DefaultChallengeLifetimeSeconds));
    }
}
