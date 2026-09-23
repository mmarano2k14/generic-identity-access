using IdentityAccess.Domain;
using IdentityAccess.Mfa.Totp;

namespace IdentityAccess.Tests.Mfa.Totp
{
    public sealed class TotpAuthenticationFactorServiceTests
    {
        [Fact]
        public async Task Enrollment_confirmation_verification_and_replay_are_deterministic()
        {
            var scopeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var application = new ApplicationKey("admin-web");
            var time = new TotpTestTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_010));
            var store = new TotpTestStore();
            var audit = new TotpTestSecurityAuditWriter();
            var service = new TotpAuthenticationFactorService(
                new TotpTestRouteResolver(),
                store,
                new TotpTestSecretProtector(),
                audit,
                time,
                new TotpProviderOptions("Generic Identity Access", allowedClockSkewSteps: 1));

            var enrollment = await service.BeginEnrollmentAsync(
                scopeId,
                application,
                userId,
                "Primary authenticator",
                "user@example.test",
                CancellationToken.None);

            Assert.Equal(UserAuthenticatorStatus.Pending, store.Status);
            Assert.StartsWith("otpauth://totp/", enrollment.ProvisioningUri);
            Assert.DoesNotContain(" ", enrollment.Secret);
            Assert.NotNull(store.ProtectedSecret);

            var confirmationCode = TotpCodeGenerator.Generate(
                store.ProtectedSecret!,
                time.GetUtcNow().ToUnixTimeSeconds(),
                TotpProviderOptions.Digits,
                TotpProviderOptions.PeriodSeconds);

            var confirmed = await service.ConfirmEnrollmentAsync(
                scopeId,
                application,
                userId,
                enrollment.AuthenticatorId,
                confirmationCode,
                CancellationToken.None);

            Assert.Equal(TotpConfirmationResult.Confirmed, confirmed);
            Assert.Equal(UserAuthenticatorStatus.Active, store.Status);

            var replayOfConfirmationStep = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                enrollment.AuthenticatorId,
                confirmationCode,
                CancellationToken.None);

            Assert.Equal(TotpVerificationResult.ReplayDetected, replayOfConfirmationStep);

            time.Advance(TimeSpan.FromSeconds(TotpProviderOptions.PeriodSeconds));
            var nextCode = TotpCodeGenerator.Generate(
                store.ProtectedSecret!,
                time.GetUtcNow().ToUnixTimeSeconds(),
                TotpProviderOptions.Digits,
                TotpProviderOptions.PeriodSeconds);

            var verified = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                enrollment.AuthenticatorId,
                nextCode,
                CancellationToken.None);
            var replayed = await service.VerifyAsync(
                scopeId,
                application,
                userId,
                enrollment.AuthenticatorId,
                nextCode,
                CancellationToken.None);

            Assert.Equal(TotpVerificationResult.Succeeded, verified);
            Assert.Equal(TotpVerificationResult.ReplayDetected, replayed);
            Assert.Contains(audit.Events, item =>
                item.EventType == IdentityAccess.Application.Security.SecurityAuditEventType.AuthenticationFactorVerificationSucceeded);
        }

        [Fact]
        public async Task Invalid_confirmation_code_does_not_activate_authenticator()
        {
            var scopeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var application = new ApplicationKey("admin-web");
            var time = new TotpTestTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_700_000_010));
            var store = new TotpTestStore();
            var service = new TotpAuthenticationFactorService(
                new TotpTestRouteResolver(),
                store,
                new TotpTestSecretProtector(),
                new TotpTestSecurityAuditWriter(),
                time,
                new TotpProviderOptions("Generic Identity Access", allowedClockSkewSteps: 1));

            var enrollment = await service.BeginEnrollmentAsync(
                scopeId,
                application,
                userId,
                "Primary authenticator",
                "user@example.test",
                CancellationToken.None);

            var validCode = TotpCodeGenerator.Generate(
                store.ProtectedSecret!,
                time.GetUtcNow().ToUnixTimeSeconds(),
                TotpProviderOptions.Digits,
                TotpProviderOptions.PeriodSeconds);
            var invalidCode = (validCode[0] == '0' ? '1' : '0') + validCode[1..];

            var result = await service.ConfirmEnrollmentAsync(
                scopeId,
                application,
                userId,
                enrollment.AuthenticatorId,
                invalidCode,
                CancellationToken.None);

            Assert.Equal(TotpConfirmationResult.InvalidCode, result);
            Assert.Equal(UserAuthenticatorStatus.Pending, store.Status);
        }
    }
}
