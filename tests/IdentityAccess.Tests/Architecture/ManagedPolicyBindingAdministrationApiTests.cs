using IdentityAccess.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Tests.Architecture
{
    public sealed class ManagedPolicyBindingAdministrationApiTests
    {
        [Fact]
        public void Managed_policy_binding_route_is_tenant_scoped_while_policy_catalog_identity_remains_shared()
        {
            var route = typeof(ManagedPolicyBindingsController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>()
                .Single();

            Assert.Equal(
                "api/v1/identity-scopes/{identityScopeId:guid}/tenants/{tenantId:guid}/applications/{applicationKey}/managed-policy-bindings",
                route.Template);
        }

        [Theory]
        [InlineData("ListAvailablePolicies")]
        [InlineData("List")]
        [InlineData("Add")]
        [InlineData("Remove")]
        public void Managed_policy_binding_actions_are_explicit_controller_operations(string methodName)
        {
            Assert.NotNull(typeof(ManagedPolicyBindingsController).GetMethod(methodName));
        }
    }
}
