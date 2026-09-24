using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Routing;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa
{
    public sealed class MfaProviderPolicyGuardTests
    {
        private static readonly Guid ScopeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly ApplicationKey Application = new("admin-web");
        private static readonly AuthenticationFactorProviderKey Totp = new("totp");

        [Fact]
        public async Task Missing_policy_fails_closed()
        {
            var guard = new MfaProviderPolicyGuard(new MfaTestPolicyStore());

            var decision = await guard.EvaluateAsync(
                Route(), ScopeId, Application, Totp, CancellationToken.None);

            Assert.Equal(MfaProviderPolicyDecision.PolicyNotConfigured, decision);
        }

        [Fact]
        public async Task Disabled_policy_rejects_provider()
        {
            var store = new MfaTestPolicyStore
            {
                Policy = new VersionedRecord<MfaPolicy>(
                    new MfaPolicy(ScopeId, Application, MfaPolicyMode.Disabled, []),
                    1)
            };
            var guard = new MfaProviderPolicyGuard(store);

            var decision = await guard.EvaluateAsync(
                Route(), ScopeId, Application, Totp, CancellationToken.None);

            Assert.Equal(MfaProviderPolicyDecision.MfaDisabled, decision);
        }

        [Fact]
        public async Task Enabled_policy_allows_only_explicit_provider()
        {
            var store = new MfaTestPolicyStore
            {
                Policy = new VersionedRecord<MfaPolicy>(
                    new MfaPolicy(ScopeId, Application, MfaPolicyMode.Required, [Totp]),
                    1)
            };
            var guard = new MfaProviderPolicyGuard(store);

            var allowed = await guard.EvaluateAsync(
                Route(), ScopeId, Application, Totp, CancellationToken.None);
            var denied = await guard.EvaluateAsync(
                Route(), ScopeId, Application, new AuthenticationFactorProviderKey("recovery"), CancellationToken.None);

            Assert.Equal(MfaProviderPolicyDecision.Allowed, allowed);
            Assert.Equal(MfaProviderPolicyDecision.ProviderNotAllowed, denied);
        }

        [Fact]
        public async Task Route_scope_mismatch_is_rejected_before_policy_read()
        {
            var guard = new MfaProviderPolicyGuard(new MfaTestPolicyStore());
            var wrongRoute = new ResolvedDatabaseRoute(
                new DatabaseRouteRequest(Application, Guid.Parse("22222222-2222-2222-2222-222222222222")),
                "identity-test",
                new ConnectionSecretReference("env:test"),
                1,
                1);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                guard.EvaluateAsync(wrongRoute, ScopeId, Application, Totp, CancellationToken.None));
        }

        private static ResolvedDatabaseRoute Route() =>
            new(
                new DatabaseRouteRequest(Application, ScopeId),
                "identity-test",
                new ConnectionSecretReference("env:test"),
                1,
                1);
    }
}
