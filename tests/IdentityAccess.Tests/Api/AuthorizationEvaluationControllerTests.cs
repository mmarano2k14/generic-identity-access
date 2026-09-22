using IdentityAccess.Api.Controllers;
using IdentityAccess.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Tests.Api
{
    /// <summary>Verifies the public capability-evaluation response preserves authorization semantics.</summary>
    public sealed class AuthorizationEvaluationControllerTests
    {
        /// <summary>Verifies a normal RBAC allow is returned as an explicit true result.</summary>
        [Fact]
        public async Task Allowed_result_returns_true()
        {
            var authorizer = new FixedAdministrationRequestAuthorizer(
                AdministrationAccessResult.Allow());

            var controller = Create(authorizer);
            var result = await controller.EvaluateTenant(
                new AuthorizationEvaluationRequest(
                    "Billing",
                    "Invoice",
                    "Refund"),
                TestContext.Current.CancellationToken);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthorizationEvaluationResponse>(ok.Value);

            Assert.True(response.Allowed);
            Assert.Equal("billing", authorizer.Resource);
            Assert.Equal("invoice", authorizer.Feature);
            Assert.Equal("refund", authorizer.Action);
        }

        /// <summary>Verifies an explicit RBAC deny is data, not a transport-level forbidden error.</summary>
        [Fact]
        public async Task Explicit_denial_returns_false()
        {
            var controller = Create(
                new FixedAdministrationRequestAuthorizer(
                    AdministrationAccessResult.Deny()));

            var result = await controller.EvaluateTenant(
                Request(),
                TestContext.Current.CancellationToken);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthorizationEvaluationResponse>(ok.Value);

            Assert.False(response.Allowed);
        }

        /// <summary>Verifies an authenticated boundary mismatch remains forbidden.</summary>
        [Fact]
        public async Task Boundary_mismatch_returns_forbidden()
        {
            var controller = Create(
                new FixedAdministrationRequestAuthorizer(
                    AdministrationAccessResult.Deny(
                        AdministrationAccessFailureCode.AuthenticationContextMismatch)));

            var result = await controller.EvaluateTenant(
                Request(),
                TestContext.Current.CancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        /// <summary>Verifies missing authentication remains unauthorized.</summary>
        [Fact]
        public async Task Unauthenticated_result_returns_unauthorized()
        {
            var controller = Create(
                new FixedAdministrationRequestAuthorizer(
                    AdministrationAccessResult.Unauthenticated(
                        AdministrationAccessFailureCode.AuthenticationRequired)));

            var result = await controller.EvaluateIdentityScope(
                Request(),
                TestContext.Current.CancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        }

        /// <summary>Verifies technical authorization failures remain unavailable.</summary>
        [Fact]
        public async Task Technical_failure_returns_service_unavailable()
        {
            var controller = Create(
                new FixedAdministrationRequestAuthorizer(
                    AdministrationAccessResult.Unavailable(
                        AdministrationAccessFailureCode.AuthorizationTechnicalFailure)));

            var result = await controller.EvaluateResourceScope(
                Request(),
                TestContext.Current.CancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        }

        /// <summary>Verifies malformed capability segments are rejected before authorization.</summary>
        [Fact]
        public async Task Invalid_capability_returns_bad_request()
        {
            var authorizer = new FixedAdministrationRequestAuthorizer(
                AdministrationAccessResult.Allow());

            var controller = Create(authorizer);
            var result = await controller.EvaluateTenant(
                new AuthorizationEvaluationRequest(
                    "*",
                    "invoice",
                    "refund"),
                TestContext.Current.CancellationToken);

            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            Assert.Null(authorizer.Resource);
        }

        private static AuthorizationEvaluationController Create(
            FixedAdministrationRequestAuthorizer authorizer) =>
            new(authorizer)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

        private static AuthorizationEvaluationRequest Request() =>
            new(
                "identity-access",
                "group",
                "read");
    }
}
