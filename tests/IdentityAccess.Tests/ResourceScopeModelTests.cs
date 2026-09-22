using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{

    public sealed class ResourceScopeModelTests
    {
        [Fact]
        public void Root_type_can_attach_to_tenant_and_child_type_requires_parent_type()
        {
            var scope = Guid.NewGuid();
            var app = new ApplicationKey("app-a");
            var model = new ApplicationSecurityModelReference(scope, app, 1);
            var organization = new ApplicationScopeTypeDefinition(model, new ResourceScopeTypeKey("organization"),
                "Organization", null, true);
            var business = new ApplicationScopeTypeDefinition(model, new ResourceScopeTypeKey("business"),
                "Business", new ResourceScopeTypeKey("organization"), false);

            Assert.True(organization.CanAttachToTenant);
            Assert.Null(organization.ParentType);
            Assert.Equal("organization", business.ParentType!.Value);
        }

        [Fact]
        public void Child_type_cannot_attach_directly_to_tenant()
        {
            var model = new ApplicationSecurityModelReference(Guid.NewGuid(), new ApplicationKey("app-a"), 1);
            Assert.Throws<ArgumentException>(() => new ApplicationScopeTypeDefinition(model,
                new ResourceScopeTypeKey("business"), "Business", new ResourceScopeTypeKey("organization"), true));
        }

        [Fact]
        public void Resource_scope_keeps_tenant_application_and_parent_independent_from_business_semantics()
        {
            var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
            var app = new ApplicationKey("app-a");
            var parent = Guid.NewGuid();
            var resource = new ResourceScope(new ResourceScopeReference(tenant, app, Guid.NewGuid()), 1,
                new ResourceScopeTypeKey("unit"), "unit-42", "Unit 42", parent, ResourceScopeStatus.Active);

            Assert.Equal(tenant, resource.Reference.Tenant);
            Assert.Equal(parent, resource.ParentResourceScopeId);
            Assert.Equal("unit", resource.Type.Value);
        }

        [Fact]
        public void Scoped_binding_rejects_cross_tenant_target()
        {
            var scope = Guid.NewGuid();
            var app = new ApplicationKey("app-a");
            var tenantA = new TenantReference(scope, Guid.NewGuid());
            var tenantB = new TenantReference(scope, Guid.NewGuid());
            var group = new UserGroup(new GroupReference(tenantA, app, Guid.NewGuid()), "Group");
            var policy = new PermissionPolicy(new PermissionPolicyReference(tenantA, app, Guid.NewGuid()), "Policy");
            var target = new ResourceScope(new ResourceScopeReference(tenantB, app, Guid.NewGuid()), 1,
                new ResourceScopeTypeKey("unit"), "unit-a", "Unit A", null);

            Assert.Throws<ArgumentException>(() => GroupPolicyBinding.Create(group, policy, target, false));
        }

        [Fact]
        public void Tenant_wide_binding_cannot_request_descendant_expansion()
        {
            var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
            var app = new ApplicationKey("app-a");
            Assert.Throws<ArgumentException>(() => GroupPolicyBinding.Restore(
                new GroupReference(tenant, app, Guid.NewGuid()),
                new PermissionPolicyReference(tenant, app, Guid.NewGuid()), null, true));
        }
    }
}
