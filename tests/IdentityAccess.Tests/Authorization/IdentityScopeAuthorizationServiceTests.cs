using IdentityAccess.Application.Authorization;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Verifies identity-scope authorization orchestration and grant provenance.</summary>
    public sealed class IdentityScopeAuthorizationServiceTests
    {
        private static readonly Guid ScopeId =
            Guid.Parse("33111111-1111-1111-1111-111111111111");

        private static readonly SubjectReference Subject =
            new(
                ScopeId,
                Guid.Parse("33aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        private static readonly ApplicationKey Application =
            new("app-a");

        /// <summary>Verifies projected scope grants are evaluated by the external RBAC adapter.</summary>
        [Fact]
        public async Task Scope_grants_are_materialized_and_delegated()
        {
            var adapter = new IdentityScopeAuthorizationCapturingRbacAdapter(
                RbacAuthorizationResult.Allow());

            var service = Create(
                [
                    Grant(
                        new CapabilityPattern(
                            "identity-access",
                            "user",
                            "*"))
                ],
                adapter);

            var result = await service.AuthorizeAsync(
                Request(
                    new CapabilityKey(
                        "identity-access",
                        "user",
                        "write")),
                TestContext.Current.CancellationToken);

            Assert.Equal(
                IdentityAuthorizationDecision.Allowed,
                result.Decision);

            var rbacRequest = Assert.IsType<RbacAuthorizationRequest>(
                adapter.Request);

            Assert.Contains(
                "trn:admin-project:admin-namespace:identity-access:user:*",
                rbacRequest.GrantedTrns);
        }

        /// <summary>Verifies a provenance mismatch fails technically before RBAC invocation.</summary>
        [Fact]
        public async Task Grant_provenance_mismatch_fails_closed()
        {
            var adapter = new IdentityScopeAuthorizationCapturingRbacAdapter(
                RbacAuthorizationResult.Allow());

            var wrongSubject = new SubjectReference(
                ScopeId,
                Guid.Parse("33eeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            var service = Create(
                [
                    new AssignedIdentityScopeCapabilityGrant(
                        ScopeId,
                        wrongSubject,
                        Application,
                        Guid.Parse("33bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        Guid.Parse("33cccccc-cccc-cccc-cccc-cccccccccccc"),
                        Guid.Parse("33dddddd-dddd-dddd-dddd-dddddddddddd"),
                        new ApplicationSecurityModelReference(
                            ScopeId,
                            Application,
                            1),
                        new CapabilityPattern(
                            "identity-access",
                            "user",
                            "read"))
                ],
                adapter);

            var result = await service.AuthorizeAsync(
                Request(
                    new CapabilityKey(
                        "identity-access",
                        "user",
                        "read")),
                TestContext.Current.CancellationToken);

            Assert.Equal(
                IdentityAuthorizationDecision.TechnicalFailure,
                result.Decision);

            Assert.Equal(
                IdentityAuthorizationFailureCode.GrantProvenanceMismatch,
                result.FailureCode);

            Assert.Null(adapter.Request);
        }

        private static IdentityScopeAuthorizationService Create(
            IReadOnlyList<AssignedIdentityScopeCapabilityGrant> grants,
            IdentityScopeAuthorizationCapturingRbacAdapter adapter) =>
            new(
                new IdentityScopeAuthorizationTestRouteResolver(),
                new IdentityScopeAuthorizationTestGrantReader(grants),
                new RbacTrnCompiler(),
                adapter);

        private static IdentityScopeAuthorizationRequest Request(
            CapabilityKey capability) =>
            new(
                ScopeId,
                Subject,
                Application,
                "admin-project",
                "admin-namespace",
                capability);

        private static AssignedIdentityScopeCapabilityGrant Grant(
            CapabilityPattern pattern) =>
            new(
                ScopeId,
                Subject,
                Application,
                Guid.Parse("33bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Guid.Parse("33cccccc-cccc-cccc-cccc-cccccccccccc"),
                Guid.Parse("33dddddd-dddd-dddd-dddd-dddddddddddd"),
                new ApplicationSecurityModelReference(
                    ScopeId,
                    Application,
                    1),
                pattern);
    }
}
