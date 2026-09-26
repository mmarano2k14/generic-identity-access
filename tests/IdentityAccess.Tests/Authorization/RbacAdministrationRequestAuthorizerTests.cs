using IdentityAccess.Api.Security;
using IdentityAccess.Authorization;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>
    /// Verifies the trusted administration context is translated into the correct authorization
    /// boundary without conflating tenant and identity-scope authority.
    /// </summary>
    public sealed class RbacAdministrationRequestAuthorizerTests
    {
        private static readonly Guid ScopeId =
            Guid.Parse("32111111-1111-1111-1111-111111111111");

        private static readonly Guid UserId =
            Guid.Parse("32aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly Guid TenantId =
            Guid.Parse("32bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static readonly Guid ResourceScopeId =
            Guid.Parse("32cccccc-cccc-cccc-cccc-cccccccccccc");

        /// <summary>
        /// Verifies an allowed tenant/resource request reaches identity authorization with trusted
        /// subject and route target provenance.
        /// </summary>
        [Fact]
        public async Task Allowed_request_maps_trusted_context_and_route_target()
        {
            var service = new RbacAdministrationCapturingAuthorizationService(
                IdentityAuthorizationResult.Allow());

            var authorizer = Create(service);
            var httpContext = TenantRequest(
                includeResourceScope: true);

            var result = await authorizer.AuthorizeAsync(
                httpContext,
                "identity-access",
                "resource-scope",
                "read",
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Allowed,
                result.Decision);

            var request = Assert.IsType<IdentityAuthorizationRequest>(
                service.Request);

            Assert.Equal(ScopeId, request.Subject.IdentityScopeId);
            Assert.Equal(UserId, request.Subject.UserId);
            Assert.Equal(TenantId, request.Tenant.TenantId);
            Assert.Equal("app-a", request.Application.Value);
            Assert.Equal("admin-project", request.RbacProject);
            Assert.Equal("admin-namespace", request.RbacNamespace);
            Assert.Equal("identity-access", request.Capability.Resource);
            Assert.Equal("resource-scope", request.Capability.Feature);
            Assert.Equal("read", request.Capability.Action);
            Assert.Equal(
                ResourceScopeId,
                Assert.IsType<ResourceScopeReference>(
                    request.ResourceScope).ResourceScopeId);

            Assert.NotNull(
                httpContext.Features.Get<
                    AdministrationRequestContextFeature>());
        }

        /// <summary>
        /// Verifies identity-scope authority may authorize a tenant route without requiring a
        /// tenant-local administration membership.
        /// </summary>
        [Fact]
        public async Task Tenant_route_scope_allow_bypasses_tenant_authority()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var result = await Create(
                tenantService,
                scopeService).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "tenant-membership",
                    "write",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Allowed,
                result.Decision);

            Assert.Null(tenantService.Request);

            var request = Assert.IsType<IdentityScopeAuthorizationRequest>(
                scopeService.Request);

            Assert.Equal(ScopeId, request.IdentityScopeId);
            Assert.Equal("identity-access", request.Capability.Resource);
            Assert.Equal("tenant-membership", request.Capability.Feature);
            Assert.Equal("write", request.Capability.Action);
        }

        /// <summary>
        /// Verifies a scope-level denial falls back to tenant-local authority.
        /// </summary>
        [Fact]
        public async Task Tenant_route_scope_deny_falls_back_to_tenant_allow()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var result = await Create(
                tenantService,
                scopeService).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "group",
                    "write",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Allowed,
                result.Decision);

            Assert.NotNull(scopeService.Request);
            Assert.NotNull(tenantService.Request);
        }

        /// <summary>
        /// Verifies independent tenant authority can still allow when scope authorization fails
        /// technically.
        /// </summary>
        [Fact]
        public async Task Tenant_route_scope_technical_failure_with_tenant_allow_allows()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Failure(
                        IdentityAuthorizationFailureCode.RbacTechnicalFailure));

            var result = await Create(
                tenantService,
                scopeService).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "policy",
                    "read",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Allowed,
                result.Decision);
        }

        /// <summary>
        /// Verifies a scope technical failure cannot be converted to a denial when tenant
        /// authority also denies.
        /// </summary>
        [Fact]
        public async Task Tenant_route_scope_technical_failure_with_tenant_deny_is_unavailable()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Failure(
                        IdentityAuthorizationFailureCode.RbacTechnicalFailure));

            var result = await Create(
                tenantService,
                scopeService).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "policy",
                    "read",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Unavailable,
                result.Decision);

            Assert.Equal(
                AdministrationAccessFailureCode.AuthorizationTechnicalFailure,
                result.FailureCode);
        }

        /// <summary>
        /// Verifies tenant technical failure remains unavailable after a clean scope denial.
        /// </summary>
        [Fact]
        public async Task Tenant_route_scope_deny_with_tenant_technical_failure_is_unavailable()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Failure(
                        IdentityAuthorizationFailureCode.RbacTechnicalFailure));

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var result = await Create(
                tenantService,
                scopeService).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "policy",
                    "read",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Unavailable,
                result.Decision);

            Assert.Equal(
                AdministrationAccessFailureCode.AuthorizationTechnicalFailure,
                result.FailureCode);
        }

        /// <summary>
        /// Verifies an external/identity authorization denial remains an explicit HTTP-layer deny.
        /// </summary>
        [Fact]
        public async Task Denied_request_remains_denied()
        {
            var service = new RbacAdministrationCapturingAuthorizationService(
                IdentityAuthorizationResult.Deny());

            var result = await Create(service).AuthorizeAsync(
                TenantRequest(includeResourceScope: false),
                "identity-access",
                "group",
                "write",
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Denied,
                result.Decision);
        }

        /// <summary>
        /// Verifies technical authorization failure is not converted to denial or allowance.
        /// </summary>
        [Fact]
        public async Task Technical_failure_remains_unavailable()
        {
            var service = new RbacAdministrationCapturingAuthorizationService(
                IdentityAuthorizationResult.Failure(
                    IdentityAuthorizationFailureCode.RbacTechnicalFailure));

            var result = await Create(service).AuthorizeAsync(
                TenantRequest(includeResourceScope: false),
                "identity-access",
                "policy",
                "read",
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Unavailable,
                result.Decision);

            Assert.Equal(
                AdministrationAccessFailureCode.AuthorizationTechnicalFailure,
                result.FailureCode);
        }

        /// <summary>
        /// Verifies identity-scope-only administration uses the dedicated scope authorization
        /// service and does not borrow authority from a tenant.
        /// </summary>
        [Fact]
        public async Task Route_without_tenant_uses_identity_scope_authority()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var httpContext = BaseRequest();
            httpContext.Request.RouteValues["identityScopeId"] =
                ScopeId.ToString("D");
            httpContext.Request.RouteValues["applicationKey"] =
                "app-a";

            var result = await Create(
                tenantService,
                scopeService).AuthorizeAsync(
                    httpContext,
                    "identity-access",
                    "user",
                    "write",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Allowed,
                result.Decision);

            Assert.Null(tenantService.Request);

            var request = Assert.IsType<IdentityScopeAuthorizationRequest>(
                scopeService.Request);

            Assert.Equal(ScopeId, request.IdentityScopeId);
            Assert.Equal(UserId, request.Subject.UserId);
            Assert.Equal("app-a", request.Application.Value);
        }

        /// <summary>
        /// Verifies tenant-local RBAC cannot authorize a tenant outside the subject's active memberships
        /// after a clean identity-scope denial.
        /// </summary>
        [Fact]
        public async Task Tenant_route_without_active_membership_is_denied_before_tenant_rbac()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var result = await Create(
                tenantService,
                scopeService,
                hasActiveTenantMembership: false).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "group",
                    "read",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Denied,
                result.Decision);
            Assert.Equal(
                AdministrationAccessFailureCode.TenantContextOutsideVisibility,
                result.FailureCode);
            Assert.Null(tenantService.Request);
        }

        /// <summary>
        /// Verifies an unresolved scope decision is not collapsed into a deny when no tenant membership
        /// can provide an independent tenant-local authorization path.
        /// </summary>
        [Fact]
        public async Task Tenant_route_without_active_membership_preserves_scope_technical_failure()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Failure(
                        IdentityAuthorizationFailureCode.RbacTechnicalFailure));

            var result = await Create(
                tenantService,
                scopeService,
                hasActiveTenantMembership: false).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "policy",
                    "read",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Unavailable,
                result.Decision);
            Assert.Equal(
                AdministrationAccessFailureCode.AuthorizationTechnicalFailure,
                result.FailureCode);
            Assert.Null(tenantService.Request);
        }

        /// <summary>
        /// Verifies an explicit identity-scope allow remains sufficient for cross-tenant administration
        /// even when the subject has no tenant membership.
        /// </summary>
        [Fact]
        public async Task Tenant_route_scope_allow_does_not_require_tenant_membership()
        {
            var tenantService =
                new RbacAdministrationCapturingAuthorizationService(
                    IdentityAuthorizationResult.Deny());

            var scopeService =
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Allow());

            var result = await Create(
                tenantService,
                scopeService,
                hasActiveTenantMembership: false).AuthorizeAsync(
                    TenantRequest(includeResourceScope: false),
                    "identity-access",
                    "tenant-membership",
                    "read",
                    TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Allowed,
                result.Decision);
            Assert.Null(tenantService.Request);
        }

        /// <summary>
        /// Verifies route scope/application mismatch is denied before authorization orchestration.
        /// </summary>
        [Fact]
        public async Task Authenticated_context_mismatch_is_denied_before_rbac()
        {
            var service = new RbacAdministrationCapturingAuthorizationService(
                IdentityAuthorizationResult.Allow());

            var httpContext = TenantRequest(
                includeResourceScope: false);
            httpContext.Request.RouteValues["applicationKey"] =
                "app-b";

            var result = await Create(service).AuthorizeAsync(
                httpContext,
                "identity-access",
                "group",
                "read",
                TestContext.Current.CancellationToken);

            Assert.Equal(
                AdministrationAccessDecision.Denied,
                result.Decision);

            Assert.Equal(
                AdministrationAccessFailureCode.AuthenticationContextMismatch,
                result.FailureCode);

            Assert.Null(service.Request);
        }

        private static RbacAdministrationRequestAuthorizer Create(
            RbacAdministrationCapturingAuthorizationService service) =>
            Create(
                service,
                new RbacAdministrationCapturingIdentityScopeAuthorizationService(
                    IdentityAuthorizationResult.Deny()));

        private static RbacAdministrationRequestAuthorizer Create(
            RbacAdministrationCapturingAuthorizationService tenantService,
            RbacAdministrationCapturingIdentityScopeAuthorizationService scopeService,
            bool hasActiveTenantMembership = true) =>
            new(
                new RbacAdministrationTestContextResolver(
                    AdministrationAuthenticationResult.Authenticated(
                        Context())),
                tenantService,
                scopeService,
                new RbacAdministrationTenantVisibilityService(hasActiveTenantMembership),
                new AdministrationAuthorizationOptions(
                    "admin-project",
                    "admin-namespace"),
                NullLogger<RbacAdministrationRequestAuthorizer>.Instance);

        private static DefaultHttpContext TenantRequest(
            bool includeResourceScope)
        {
            var httpContext = BaseRequest();

            httpContext.Request.RouteValues["identityScopeId"] =
                ScopeId.ToString("D");
            httpContext.Request.RouteValues["tenantId"] =
                TenantId.ToString("D");
            httpContext.Request.RouteValues["applicationKey"] =
                "app-a";

            if (includeResourceScope)
            {
                httpContext.Request.RouteValues["resourceScopeId"] =
                    ResourceScopeId.ToString("D");
            }

            return httpContext;
        }

        private static DefaultHttpContext BaseRequest() =>
            new();

        private static AdministrationRequestContext Context() =>
            new(
                new SubjectReference(
                    ScopeId,
                    UserId),
                Guid.Parse(
                    "32dddddd-dddd-dddd-dddd-dddddddddddd"),
                "admin-web",
                new ApplicationKey("app-a"),
                "primary",
                DateTimeOffset.UtcNow.AddHours(1));
    }
}
