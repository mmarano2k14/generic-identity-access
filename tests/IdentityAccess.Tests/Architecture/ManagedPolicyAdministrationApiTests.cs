using IdentityAccess.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Tests.Architecture
{
    public sealed class ManagedPolicyAdministrationApiTests
    {
        [Fact]
        public void Managed_policy_catalog_route_is_application_scoped_and_not_tenant_owned()
        {
            var route = typeof(ManagedPoliciesController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>()
                .Single();

            Assert.Equal(
                "api/v1/identity-scopes/{identityScopeId:guid}/applications/{applicationKey}/managed-policies",
                route.Template);
            Assert.DoesNotContain("tenantId", route.Template ?? string.Empty);
        }

        [Theory]
        [InlineData("List")]
        [InlineData("Get")]
        [InlineData("Create")]
        [InlineData("Update")]
        [InlineData("ListVersions")]
        [InlineData("GetVersion")]
        [InlineData("CreateVersion")]
        [InlineData("PublishVersion")]
        [InlineData("ListStatements")]
        [InlineData("AddStatement")]
        [InlineData("RemoveStatement")]
        public void Managed_policy_catalog_actions_are_explicit_controller_operations(string methodName)
        {
            Assert.NotNull(typeof(ManagedPoliciesController).GetMethod(methodName));
        }
    }
}
