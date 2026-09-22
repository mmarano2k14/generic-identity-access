using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{

    public sealed class PermissionPolicyModelTests
    {
        [Fact]
        public void Capability_key_is_structured_but_does_not_define_a_trn()
        {
            var key = new CapabilityKey("billing", "invoice", "read");
            Assert.Equal("billing", key.Resource);
            Assert.Equal("invoice", key.Feature);
            Assert.Equal("read", key.Action);
        }

        [Fact]
        public void Capability_segments_are_canonicalized_for_rbac_compatibility()
        {
            var key = new CapabilityKey(" Billing ", " Invoice ", " Read ");
            Assert.Equal("billing", key.Resource);
            Assert.Equal("invoice", key.Feature);
            Assert.Equal("read", key.Action);
        }

        [Theory]
        [InlineData("billing", "invoice_item", "read")]
        [InlineData("1billing", "invoice", "read")]
        [InlineData("billing", "invoice", "read:all")]
        public void Capability_segments_reject_noncanonical_grammar(string resource, string feature, string action)
        {
            Assert.Throws<ArgumentException>(() => new CapabilityKey(resource, feature, action));
        }

        [Fact]
        public void Policy_statement_requires_same_scope_and_application_as_the_model()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var policy = new PermissionPolicyReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid());
            var model = new ApplicationSecurityModelReference(scope, new ApplicationKey("app-b"), 1);

            Assert.Throws<ArgumentException>(() => new PolicyStatement(Guid.NewGuid(), policy, model,
                new CapabilityKey("billing", "invoice", "read")));
        }

        [Fact]
        public void New_binding_requires_same_tenant_application_and_active_state()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var group = new UserGroup(new GroupReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid()), "Operators");
            var policy = new PermissionPolicy(new PermissionPolicyReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid()),
                "Read invoices");

            var binding = GroupPolicyBinding.Create(group, policy);
            Assert.Equal(group.Reference, binding.Group);
            Assert.Equal(policy.Reference, binding.Policy);
        }

        [Fact]
        public void Binding_rejects_cross_application_policy()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var group = new UserGroup(new GroupReference(tenant, new ApplicationKey("app-a"), Guid.NewGuid()), "Operators");
            var policy = new PermissionPolicy(new PermissionPolicyReference(tenant, new ApplicationKey("app-b"), Guid.NewGuid()),
                "Read invoices");

            Assert.Throws<ArgumentException>(() => GroupPolicyBinding.Create(group, policy));
        }
        [Fact]
        public void New_permission_persistence_contracts_require_explicit_cancellation_tokens()
        {
            var contracts = new[]
            {
                typeof(IdentityAccess.Application.Storage.IApplicationSecurityModelStore),
                typeof(IdentityAccess.Application.Storage.IPermissionPolicyStore),
                typeof(IdentityAccess.Application.Storage.IPolicyStatementStore),
                typeof(IdentityAccess.Application.Storage.IGroupPolicyBindingStore)
            };

            foreach (var contract in contracts)
            {
                foreach (var method in contract.GetMethods())
                {
                    var last = Assert.Single(method.GetParameters().TakeLast(1));
                    Assert.Equal(typeof(CancellationToken), last.ParameterType);
                    Assert.False(last.IsOptional);
                }
            }
        }

    }
}
