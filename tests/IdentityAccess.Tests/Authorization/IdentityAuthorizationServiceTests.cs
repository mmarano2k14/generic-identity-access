using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Authorization
{

    public sealed class IdentityAuthorizationServiceTests
    {
        internal static readonly Guid ScopeA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid ScopeB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid UserA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid GroupA = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly Guid PolicyA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        private static readonly Guid StatementA = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        [Fact]
        public async Task Allowed_decision_is_returned_only_after_route_projection_and_external_rbac()
        {
            var request = Request();
            var routeResolver = new RouteResolverFake(Route(request));
            var reader = new GrantReaderFake([Grant(request, new CapabilityKey("billing", "invoice", "read"))]);
            var adapter = new AdapterFake(rbacRequest =>
            {
                Assert.Equal("project-a", rbacRequest.Project);
                Assert.Equal("namespace-a", rbacRequest.Namespace);
                Assert.Equal(request.Capability, rbacRequest.Capability);
                Assert.Equal(["trn:project-a:namespace-a:billing:invoice:read"], rbacRequest.GrantedTrns);
                return RbacAuthorizationResult.Allow();
            });
            var service = Service(routeResolver, reader, adapter);

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.Allowed, result.Decision);
            Assert.Equal(1, routeResolver.CallCount);
            Assert.Equal(1, reader.CallCount);
            Assert.Equal(1, adapter.CallCount);
            Assert.Same(routeResolver.Route, reader.LastRoute);
        }

        [Fact]
        public async Task Published_managed_policy_grant_uses_the_same_external_rbac_evaluator()
        {
            var request = Request();
            var managedPolicy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(request.Tenant.IdentityScopeId, request.Application, PolicyA),
                3);
            var grant = new AssignedCapabilityGrant(
                request.Subject,
                request.Tenant,
                request.Application,
                new GroupReference(request.Tenant, request.Application, GroupA),
                managedPolicy,
                StatementA,
                new ApplicationSecurityModelReference(request.Tenant.IdentityScopeId, request.Application, 1),
                new CapabilityKey("billing", "invoice", "read"));
            var adapter = new AdapterFake(rbacRequest =>
            {
                Assert.Equal(["trn:project-a:namespace-a:billing:invoice:read"], rbacRequest.GrantedTrns);
                return RbacAuthorizationResult.Allow();
            });
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake([grant]),
                adapter);

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.Allowed, result.Decision);
            Assert.Equal(1, adapter.CallCount);
        }

        [Fact]
        public async Task Denial_from_external_rbac_remains_a_business_denial()
        {
            var request = Request();
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake([]),
                new AdapterFake(_ => RbacAuthorizationResult.Deny()));

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.Denied, result.Decision);
            Assert.Null(result.FailureCode);
        }

        [Fact]
        public async Task External_rbac_technical_failure_is_not_converted_to_denial()
        {
            var request = Request();
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake([]),
                new AdapterFake(_ => RbacAuthorizationResult.Failure(RbacAuthorizationFailureCode.ExternalInvocationFailed)));

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.TechnicalFailure, result.Decision);
            Assert.Equal(IdentityAuthorizationFailureCode.RbacTechnicalFailure, result.FailureCode);
            Assert.Equal(RbacAuthorizationFailureCode.ExternalInvocationFailed, result.RbacFailureCode);
        }

        [Fact]
        public async Task Route_failure_is_a_technical_failure_and_stops_before_projection()
        {
            var request = Request();
            var reader = new GrantReaderFake([]);
            var adapter = new AdapterFake(_ => RbacAuthorizationResult.Allow());
            var service = Service(new RouteResolverFake(new InvalidOperationException("route unavailable")), reader, adapter);

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.TechnicalFailure, result.Decision);
            Assert.Equal(IdentityAuthorizationFailureCode.RouteResolutionFailed, result.FailureCode);
            Assert.Equal(0, reader.CallCount);
            Assert.Equal(0, adapter.CallCount);
        }

        [Fact]
        public async Task Projection_failure_is_a_technical_failure_and_stops_before_rbac()
        {
            var request = Request();
            var adapter = new AdapterFake(_ => RbacAuthorizationResult.Allow());
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake(new InvalidOperationException("database unavailable")),
                adapter);

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.TechnicalFailure, result.Decision);
            Assert.Equal(IdentityAuthorizationFailureCode.GrantProjectionFailed, result.FailureCode);
            Assert.Equal(0, adapter.CallCount);
        }

        [Fact]
        public async Task Cross_context_grant_is_integrity_failure_not_silently_imported()
        {
            var request = Request();
            var wrongSubject = new SubjectReference(ScopeA, Guid.Parse("99999999-9999-9999-9999-999999999999"));
            var wrongGrant = new AssignedCapabilityGrant(
                wrongSubject,
                request.Tenant,
                request.Application,
                new GroupReference(request.Tenant, request.Application, GroupA),
                new ManagedPolicyVersionReference(new ManagedPolicyReference(request.Tenant.IdentityScopeId, request.Application, PolicyA), 1),
                StatementA,
                new ApplicationSecurityModelReference(ScopeA, request.Application, 1),
                new CapabilityKey("billing", "invoice", "read"));
            var adapter = new AdapterFake(_ => RbacAuthorizationResult.Allow());
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake([wrongGrant]),
                adapter);

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.TechnicalFailure, result.Decision);
            Assert.Equal(IdentityAuthorizationFailureCode.GrantProvenanceMismatch, result.FailureCode);
            Assert.Equal(0, adapter.CallCount);
        }

        [Fact]
        public async Task Duplicate_assigned_capabilities_are_materialized_once()
        {
            var request = Request();
            var capability = new CapabilityKey("billing", "invoice", "read");
            var first = Grant(request, capability, StatementA);
            var second = Grant(request, capability, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
            var adapter = new AdapterFake(rbacRequest =>
            {
                Assert.Single(rbacRequest.GrantedTrns);
                return RbacAuthorizationResult.Deny();
            });
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake([first, second]),
                adapter);

            _ = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(1, adapter.CallCount);
        }

        [Fact]
        public async Task Cancellation_propagates_and_is_not_converted_to_a_decision()
        {
            var request = Request();
            using var source = new CancellationTokenSource();
            source.Cancel();
            var service = Service(
                new RouteResolverFake(Route(request)),
                new GrantReaderFake([]),
                new AdapterFake(_ => RbacAuthorizationResult.Allow()));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await service.AuthorizeAsync(request, source.Token));
        }

        [Fact]
        public async Task Concurrent_authorization_operations_do_not_share_route_or_grant_state()
        {
            var app = new ApplicationKey("app-a");
            var requests = Enumerable.Range(0, 64).Select(i =>
            {
                var scope = i % 2 == 0 ? ScopeA : ScopeB;
                var tenant = new TenantReference(scope, Guid.NewGuid());
                var subject = new SubjectReference(scope, Guid.NewGuid());
                return new IdentityAuthorizationRequest(
                    tenant, subject, app, "project-a", "namespace-a",
                    new CapabilityKey("billing", "invoice", "read"));
            }).ToArray();

            var routeResolver = new ConcurrentRouteResolverFake();
            var reader = new ConcurrentGrantReaderFake();
            var adapter = new AdapterFake(r => r.GrantedTrns.Single().Contains(r.Project, StringComparison.Ordinal)
                ? RbacAuthorizationResult.Allow()
                : RbacAuthorizationResult.Deny());
            var service = Service(routeResolver, reader, adapter);

            var results = await Task.WhenAll(requests.Select(async request =>
                await service.AuthorizeAsync(request, TestContext.Current.CancellationToken)));

            Assert.All(results, result => Assert.Equal(IdentityAuthorizationDecision.Allowed, result.Decision));
            Assert.Equal(64, routeResolver.SeenScopes.Count);
            Assert.Equal(64, reader.SeenScopes.Count);
        }

        [Fact]
        public async Task Resource_scope_is_forwarded_to_assignment_projection_without_entering_trn_shape()
        {
            var baseRequest = Request();
            var target = new ResourceScopeReference(baseRequest.Tenant, baseRequest.Application, Guid.NewGuid());
            var request = new IdentityAuthorizationRequest(baseRequest.Tenant, baseRequest.Subject,
                baseRequest.Application, baseRequest.RbacProject, baseRequest.RbacNamespace,
                baseRequest.Capability, target);
            var reader = new GrantReaderFake([]);
            var service = Service(new RouteResolverFake(Route(request)), reader,
                new AdapterFake(_ => RbacAuthorizationResult.Deny()));

            _ = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(target, reader.LastResourceScope);
        }

        [Fact]
        public void Authorization_contract_requires_explicit_cancellation_token()
        {
            var method = typeof(IIdentityAuthorizationService).GetMethod(nameof(IIdentityAuthorizationService.AuthorizeAsync));
            Assert.NotNull(method);
            var parameter = method!.GetParameters().Single(x => x.ParameterType == typeof(CancellationToken));
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
        }

        [Fact]
        public async Task Persisted_wildcard_grant_is_materialized_for_external_rbac_without_local_evaluation()
        {
            var request = new IdentityAuthorizationRequest(
                new TenantReference(ScopeA, TenantA),
                new SubjectReference(ScopeA, UserA),
                new ApplicationKey("app-a"),
                "project-a",
                "namespace-a",
                new CapabilityKey("billing", "invoice", "refund"));
            var grant = Grant(request, new CapabilityPattern("billing", "invoice", "*"));
            var reader = new GrantReaderFake([grant]);
            var adapter = new AdapterFake(rbacRequest =>
            {
                Assert.Contains("trn:project-a:namespace-a:billing:invoice:*", rbacRequest.GrantedTrns);
                return RbacAuthorizationResult.Allow();
            });
            var service = Service(new RouteResolverFake(Route(request)), reader, adapter);

            var result = await service.AuthorizeAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(IdentityAuthorizationDecision.Allowed, result.Decision);
        }

        private static IdentityAuthorizationService Service(
            IDatabaseRouteResolver routeResolver,
            IAssignedCapabilityReader reader,
            IRbacAuthorizationAdapter adapter) =>
            new(routeResolver, reader, new RbacTrnCompiler(), adapter);

        internal static IdentityAuthorizationRequest Request() =>
            new(
                new TenantReference(ScopeA, TenantA),
                new SubjectReference(ScopeA, UserA),
                new ApplicationKey("app-a"),
                "project-a",
                "namespace-a",
                new CapabilityKey("billing", "invoice", "read"));

        internal static ResolvedDatabaseRoute Route(IdentityAuthorizationRequest request) =>
            new(
                new DatabaseRouteRequest(request.Application, request.Tenant.IdentityScopeId),
                "identity-default",
                new ConnectionSecretReference("env:IDENTITY_ACCESS_POSTGRES_DEFAULT"),
                1,
                1);

        private static AssignedCapabilityGrant Grant(
            IdentityAuthorizationRequest request,
            CapabilityKey capability,
            Guid? statementId = null) =>
            new(
                request.Subject,
                request.Tenant,
                request.Application,
                new GroupReference(request.Tenant, request.Application, GroupA),
                new ManagedPolicyVersionReference(new ManagedPolicyReference(request.Tenant.IdentityScopeId, request.Application, PolicyA), 1),
                statementId ?? StatementA,
                new ApplicationSecurityModelReference(request.Tenant.IdentityScopeId, request.Application, 1),
                capability);

        private static AssignedCapabilityGrant Grant(
            IdentityAuthorizationRequest request,
            CapabilityPattern pattern,
            Guid? statementId = null) =>
            new(
                request.Subject,
                request.Tenant,
                request.Application,
                new GroupReference(request.Tenant, request.Application, GroupA),
                new ManagedPolicyVersionReference(new ManagedPolicyReference(request.Tenant.IdentityScopeId, request.Application, PolicyA), 1),
                statementId ?? StatementA,
                new ApplicationSecurityModelReference(request.Tenant.IdentityScopeId, request.Application, 1),
                pattern);










    }
}
