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
            RbacAdministrationCapturingIdentityScopeAuthorizationService scopeService) =>
            new(
                new RbacAdministrationTestContextResolver(
                    AdministrationAuthenticationResult.Authenticated(
                        Context())),
                tenantService,
                scopeService,
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
