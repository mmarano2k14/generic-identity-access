using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{
    public sealed class ManagedGroupPolicyBindingModelTests
    {
        [Fact]
        public void Binding_accepts_shared_policy_without_tenant_ownership()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var application = new ApplicationKey("admin-app");
            var group = new GroupReference(tenant, application, Guid.NewGuid());
            var policy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, application, Guid.NewGuid()),
                2);

            var binding = ManagedGroupPolicyBinding.Restore(group, policy, null, false);

            Assert.Equal(tenant, binding.Group.Tenant);
            Assert.DoesNotContain(binding.PolicyVersion.Policy.GetType().GetProperties(), property =>
                property.Name.Contains("Tenant", StringComparison.Ordinal));
            Assert.Equal(2, binding.PolicyVersion.Version);
        }

        [Fact]
        public void Binding_rejects_managed_policy_from_another_identity_scope()
        {
            var tenant = new TenantReference(Guid.NewGuid(), Guid.NewGuid());
            var application = new ApplicationKey("admin-app");
            var group = new GroupReference(tenant, application, Guid.NewGuid());
            var policy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(Guid.NewGuid(), application, Guid.NewGuid()),
                1);

            Assert.Throws<ArgumentException>(() =>
                ManagedGroupPolicyBinding.Restore(group, policy, null, false));
        }

        [Fact]
        public void Binding_rejects_resource_scope_from_another_tenant()
        {
            var scope = Guid.NewGuid();
            var application = new ApplicationKey("admin-app");
            var groupTenant = new TenantReference(scope, Guid.NewGuid());
            var group = new GroupReference(groupTenant, application, Guid.NewGuid());
            var policy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, application, Guid.NewGuid()),
                1);
            var otherScope = new ResourceScopeReference(
                new TenantReference(scope, Guid.NewGuid()),
                application,
                Guid.NewGuid());

            Assert.Throws<ArgumentException>(() =>
                ManagedGroupPolicyBinding.Restore(group, policy, otherScope, false));
        }

        [Fact]
        public void Tenant_wide_binding_cannot_include_descendants()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var application = new ApplicationKey("admin-app");
            var group = new GroupReference(tenant, application, Guid.NewGuid());
            var policy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, application, Guid.NewGuid()),
                1);

            Assert.Throws<ArgumentException>(() =>
                ManagedGroupPolicyBinding.Restore(group, policy, null, true));
        }
    }
}
