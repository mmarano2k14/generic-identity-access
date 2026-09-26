using System.Net;
using System.Net.Http.Json;
using IdentityAccess.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityAccess.Tests
{

    public sealed class ControllerApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Swagger_ui_and_openapi_document_are_exposed_in_testing()
        {
            using var client = factory.CreateClient();
            using var ui = await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
            using var document = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, document.StatusCode);
            var json = await document.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("Generic Identity & Access API", json);
            Assert.Contains("/health/live", json);
            Assert.Contains("/api/v1/identity-scopes/", json);
        }

        [Fact]
        public async Task Managed_policy_administrative_controller_exists_but_fails_closed_without_trusted_authorizer()
        {
            using var client = factory.CreateClient();
            var path = $"/api/v1/identity-scopes/{Guid.NewGuid():D}/applications/app-a/managed-policies/{Guid.NewGuid():D}";
            using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public void Public_api_is_controller_based()
        {
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.HealthController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.SystemController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.ManagedPoliciesController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.PoliciesController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.PolicyBindingsController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.NonControllerAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.UsersController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), inherit: true).SingleOrDefault());
            Assert.NotNull(typeof(IdentityAccess.Api.Controllers.GroupsController).GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), inherit: true).SingleOrDefault());
        }
    }
}
