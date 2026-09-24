using IdentityAccess.Application.Security;
using IdentityAccess.Domain;
using IdentityAccess.Mfa.Recovery;

namespace IdentityAccess.Tests.Mfa.Recovery
{
    public sealed class RecoveryAuthenticationFactorServiceTests
    {
        [Fact]
        public async Task Generation_returns_raw_codes_once_but_store_receives_hashes_only()
        {
            var store = new RecoveryTestStore();
            var audit = new RecoveryTestSecurityAuditWriter();
            var service = CreateService(store, audit);
            var scopeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            var result = await service.GenerateOrReplaceAsync(
                scopeId,
                new ApplicationKey("admin-web"),
                userId,
                "Recovery codes",
                CancellationToken.None);

            Assert.Equal(10, result.Codes.Count);
            Assert.False(result.ReplacedExistingSet);
            var stored = Assert.Single(store.Sets).Value;
            Assert.Equal(10, stored.Consumed.Count);
            Assert.All(stored.Consumed.Keys, hash => Assert.Equal(64, hash.Length));
            Assert.DoesNotContain(result.Codes, code => stored.Consumed.ContainsKey(code));
            Assert.Contains(audit.Events, item => item.EventType == SecurityAuditEventType.UserAuthenticatorEnrollmentConfirmed);
        }

        [Fact]
        public async Task Valid_code_is_consumed_once_and_replay_is_rejected()
        {
            var store = new RecoveryTestStore();
            var audit = new RecoveryTestSecurityAuditWriter();
            var service = CreateService(store, audit);
            var scopeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var application = new ApplicationKey("admin-web");
            var set = await service.GenerateOrReplaceAsync(
                scopeId,
                application,
                userId,
                "Recovery codes",
                CancellationToken.None);

            var first = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                set.AuthenticatorId,
                set.Codes[0],
                CancellationToken.None);
            var second = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                set.AuthenticatorId,
                set.Codes[0],
                CancellationToken.None);

            Assert.Equal(RecoveryCodeVerificationResult.Succeeded, first);
            Assert.Equal(RecoveryCodeVerificationResult.AlreadyConsumed, second);
            Assert.Contains(audit.Events, item => item.EventType == SecurityAuditEventType.AuthenticationFactorVerificationSucceeded);
            Assert.Contains(audit.Events, item => item.ReasonCode == SecurityAuditReasonCode.AuthenticationFactorReplayDetected);
        }

        [Fact]
        public async Task Regeneration_revokes_old_set_before_new_set_becomes_active()
        {
            var store = new RecoveryTestStore();
            var audit = new RecoveryTestSecurityAuditWriter();
            var time = new RecoveryTestTimeProvider(DateTimeOffset.Parse("2026-09-23T12:00:00Z"));
            var service = CreateService(store, audit, time);
            var scopeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");
            var application = new ApplicationKey("admin-web");
            var first = await service.GenerateOrReplaceAsync(
                scopeId,
                application,
                userId,
                "Recovery codes",
                CancellationToken.None);

            time.Advance(TimeSpan.FromMinutes(1));
            var second = await service.GenerateOrReplaceAsync(
                scopeId,
                application,
                userId,
                "Recovery codes",
                CancellationToken.None);

            var oldVerification = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                first.AuthenticatorId,
                first.Codes[1],
                CancellationToken.None);
            var newVerification = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                second.AuthenticatorId,
                second.Codes[1],
                CancellationToken.None);

            Assert.True(second.ReplacedExistingSet);
            Assert.Equal(RecoveryCodeVerificationResult.NotActive, oldVerification);
            Assert.Equal(RecoveryCodeVerificationResult.Succeeded, newVerification);
            Assert.Equal(UserAuthenticatorStatus.Revoked, store.Sets[first.AuthenticatorId].Authenticator.Status);
            Assert.Contains(audit.Events, item => item.EventType == SecurityAuditEventType.UserAuthenticatorEnrollmentConfirmed);
        }

        [Fact]
        public async Task Malformed_code_is_rejected_without_store_consumption()
        {
            var store = new RecoveryTestStore();
            var audit = new RecoveryTestSecurityAuditWriter();
            var service = CreateService(store, audit);
            var scopeId = Guid.Parse("77777777-7777-7777-7777-777777777777");
            var userId = Guid.Parse("88888888-8888-8888-8888-888888888888");
            var application = new ApplicationKey("admin-web");
            var set = await service.GenerateOrReplaceAsync(
                scopeId,
                application,
                userId,
                "Recovery codes",
                CancellationToken.None);

            var result = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                set.AuthenticatorId,
                "invalid",
                CancellationToken.None);

            Assert.Equal(RecoveryCodeVerificationResult.InvalidCode, result);
            Assert.Contains(audit.Events, item => item.ReasonCode == SecurityAuditReasonCode.InvalidAuthenticationFactorProof);
        }

        private static RecoveryAuthenticationFactorService CreateService(
            RecoveryTestStore store,
            RecoveryTestSecurityAuditWriter audit,
            RecoveryTestTimeProvider? time = null) =>
            new(
                new RecoveryTestRouteResolver(),
                store,
                audit,
                new IdentityAccess.Tests.Mfa.AllowingMfaProviderPolicyGuard(),
                time ?? new RecoveryTestTimeProvider(DateTimeOffset.Parse("2026-09-23T12:00:00Z")),
                new RecoveryCodeProviderOptions(10));
    }
}
