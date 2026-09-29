using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Administration
{
    public sealed class TenantGroupAssignmentDelegationGuardTests
    {
        private static readonly Guid ScopeId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
        private static readonly Guid TenantId = Guid.Parse("a1000000-0000-0000-0000-000000000002");
        private static readonly Guid TargetTenantId = Guid.Parse("a1000000-0000-0000-0000-000000000007");
        private static readonly Guid UserId = Guid.Parse("a1000000-0000-0000-0000-000000000003");
        private static readonly Guid GroupId = Guid.Parse("a1000000-0000-0000-0000-000000000004");
        private static readonly ApplicationKey App = new("admin-web");

        [Theory]
        [InlineData(IdentityAuthorizationDecision.Allowed, AdministrationAccessDecision.Allowed)]
        [InlineData(IdentityAuthorizationDecision.Denied, AdministrationAccessDecision.Denied)]
        public async Task Direct_membership_creation_tracks_identity_scope_authority(
            IdentityAuthorizationDecision authorizationDecision,
            AdministrationAccessDecision expectedDecision)
        {
            var authorizationResult = authorizationDecision == IdentityAuthorizationDecision.Allowed
                ? IdentityAuthorizationResult.Allow()
                : IdentityAuthorizationResult.Deny();
            var guard = new TenantMembershipCreationAuthorizationGuard(
                new TenantGroupAssignmentFakeScopeAuthorization(authorizationResult),
                new AdministrationAuthorizationOptions("identity-access", "administration"));

            var result = await guard.AuthorizeDirectCreateAsync(Context(), TestContext.Current.CancellationToken);

            Assert.Equal(expectedDecision, result.Decision);
        }

        [Fact]
        public async Task Identity_scope_authority_can_assign_without_group_subset_projection()
        {
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Deny());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Allow()));

            var result = await guard.AuthorizeAssignmentAsync(Context(), TenantId, App, GroupId, TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Allowed, result.Decision);
            Assert.Equal(0, reader.CallCount);
            Assert.Equal(0, tenant.CallCount);
        }

        [Fact]
        public async Task Tenant_delegation_requires_actor_to_hold_every_concrete_group_capability()
        {
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([
                new GroupCapabilityGrant(new CapabilityPattern("billing", "invoice", "read"), null, false)
            ]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Allow());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Deny()));

            var result = await guard.AuthorizeAssignmentAsync(Context(), TenantId, App, GroupId, TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Allowed, result.Decision);
            Assert.Equal(1, reader.CallCount);
            Assert.Equal(1, tenant.CallCount);
            Assert.Equal("billing", tenant.Request!.Capability.Resource);
        }

        [Fact]
        public async Task Tenant_delegation_denies_group_capability_the_actor_does_not_hold()
        {
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([
                new GroupCapabilityGrant(new CapabilityPattern("billing", "invoice", "refund"), null, false)
            ]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Deny());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Deny()));

            var result = await guard.AuthorizeAssignmentAsync(Context(), TenantId, App, GroupId, TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Denied, result.Decision);
        }

        [Fact]
        public async Task Clone_delegation_reads_source_grants_and_checks_them_against_target_tenant()
        {
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([
                new GroupCapabilityGrant(new CapabilityPattern("billing", "invoice", "read"), null, false)
            ]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Allow());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Deny()));

            var result = await guard.AuthorizeGrantCopyAsync(
                Context(), TenantId, TargetTenantId, App, GroupId,
                new Dictionary<Guid, Guid>(),
                TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Allowed, result.Decision);
            Assert.Equal(TenantId, reader.LastGroup!.Tenant.TenantId);
            Assert.Equal(TargetTenantId, tenant.Request!.Tenant.TenantId);
            Assert.Null(tenant.Request.ResourceScope);
        }

        [Fact]
        public async Task Clone_delegation_translates_resource_scope_to_target_tenant()
        {
            var resourceScopeId = Guid.Parse("a1000000-0000-0000-0000-000000000008");
            var targetResourceScopeId = Guid.Parse("a1000000-0000-0000-0000-000000000009");
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([
                new GroupCapabilityGrant(
                    new CapabilityPattern("billing", "invoice", "read"),
                    new ResourceScopeReference(new TenantReference(ScopeId, TenantId), App, resourceScopeId),
                    false)
            ]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Allow());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Deny()));

            var result = await guard.AuthorizeGrantCopyAsync(
                Context(), TenantId, TargetTenantId, App, GroupId,
                new Dictionary<Guid, Guid> { [resourceScopeId] = targetResourceScopeId },
                TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Allowed, result.Decision);
            Assert.NotNull(tenant.Request!.ResourceScope);
            Assert.Equal(TargetTenantId, tenant.Request.ResourceScope!.Tenant.TenantId);
            Assert.Equal(targetResourceScopeId, tenant.Request.ResourceScope.ResourceScopeId);
        }

        [Fact]
        public async Task Clone_delegation_denies_scoped_grant_without_target_mapping()
        {
            var resourceScopeId = Guid.Parse("a1000000-0000-0000-0000-000000000008");
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([
                new GroupCapabilityGrant(
                    new CapabilityPattern("billing", "invoice", "read"),
                    new ResourceScopeReference(new TenantReference(ScopeId, TenantId), App, resourceScopeId),
                    false)
            ]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Allow());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Deny()));

            var result = await guard.AuthorizeGrantCopyAsync(
                Context(), TenantId, TargetTenantId, App, GroupId,
                new Dictionary<Guid, Guid>(),
                TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Denied, result.Decision);
            Assert.Equal(0, tenant.CallCount);
        }

        [Theory]
        [InlineData("billing", "invoice", "*", false)]
        [InlineData("billing", "invoice", "read", true)]
        public async Task Tenant_delegation_fails_closed_for_broad_group_grants(
            string resource, string feature, string action, bool includeDescendants)
        {
            ResourceScopeReference? scope = includeDescendants
                ? new ResourceScopeReference(new TenantReference(ScopeId, TenantId), App,
                    Guid.Parse("a1000000-0000-0000-0000-000000000005"))
                : null;
            var reader = new TenantGroupAssignmentFakeGroupGrantReader([
                new GroupCapabilityGrant(new CapabilityPattern(resource, feature, action), scope, includeDescendants)
            ]);
            var tenant = new TenantGroupAssignmentFakeTenantAuthorization(IdentityAuthorizationResult.Allow());
            var guard = Guard(reader, tenant, new TenantGroupAssignmentFakeScopeAuthorization(IdentityAuthorizationResult.Deny()));

            var result = await guard.AuthorizeAssignmentAsync(Context(), TenantId, App, GroupId, TestContext.Current.CancellationToken);

            Assert.Equal(AdministrationAccessDecision.Denied, result.Decision);
            Assert.Equal(0, tenant.CallCount);
        }

        private static TenantGroupAssignmentDelegationGuard Guard(
            IGroupCapabilityGrantReader reader,
            IIdentityAuthorizationService tenant,
            IIdentityScopeAuthorizationService scope) =>
            new(
                new TenantGroupAssignmentFakeRouteResolver(Route()),
                reader,
                tenant,
                scope,
                new AdministrationAuthorizationOptions("identity-access", "administration"));

        private static AdministrationRequestContext Context() =>
            new(
                new SubjectReference(ScopeId, UserId),
                Guid.Parse("a1000000-0000-0000-0000-000000000006"),
                "admin-web",
                App,
                "admin-web-primary",
                DateTimeOffset.UtcNow.AddHours(1));

        private static ResolvedDatabaseRoute Route() =>
            new(
                new DatabaseRouteRequest(App, ScopeId),
                "identity-a",
                new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
                1,
                1);

    }
}
