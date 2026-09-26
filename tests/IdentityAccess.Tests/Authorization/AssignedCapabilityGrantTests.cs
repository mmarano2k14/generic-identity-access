using IdentityAccess.Application.Authorization;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authorization
{

    public sealed class AssignedCapabilityGrantTests
    {
        [Fact]
        public void Grant_preserves_full_assignment_provenance_without_becoming_an_authorization_decision()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var app = new ApplicationKey("app-a");
            var subject = new SubjectReference(scope, Guid.NewGuid());
            var group = new GroupReference(tenant, app, Guid.NewGuid());
            var policy = new ManagedPolicyVersionReference(new ManagedPolicyReference(scope, app, Guid.NewGuid()), 1);
            var statementId = Guid.NewGuid();
            var model = new ApplicationSecurityModelReference(scope, app, 3);
            var capability = new CapabilityKey("billing", "invoice", "read");

            var grant = new AssignedCapabilityGrant(subject, tenant, app, group, policy, statementId, model, capability);

            Assert.Equal(subject, grant.Subject);
            Assert.Equal(group, grant.Group);
            Assert.Equal(policy, grant.ManagedPolicyVersion);
            Assert.Equal(statementId, grant.StatementId);
            Assert.Equal(model, grant.Model);
            Assert.Equal(new CapabilityPattern(capability), grant.Pattern);
        }

        [Fact]
        public void Grant_rejects_cross_scope_subject()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var app = new ApplicationKey("app-a");

            Assert.Throws<ArgumentException>(() => new AssignedCapabilityGrant(
                new SubjectReference(Guid.NewGuid(), Guid.NewGuid()),
                tenant,
                app,
                new GroupReference(tenant, app, Guid.NewGuid()),
                new ManagedPolicyVersionReference(new ManagedPolicyReference(scope, app, Guid.NewGuid()), 1),
                Guid.NewGuid(),
                new ApplicationSecurityModelReference(scope, app, 1),
                new CapabilityKey("billing", "invoice", "read")));
        }

        [Fact]
        public void Grant_rejects_cross_application_provenance()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var appA = new ApplicationKey("app-a");
            var appB = new ApplicationKey("app-b");

            Assert.Throws<ArgumentException>(() => new AssignedCapabilityGrant(
                new SubjectReference(scope, Guid.NewGuid()),
                tenant,
                appA,
                new GroupReference(tenant, appB, Guid.NewGuid()),
                new ManagedPolicyVersionReference(new ManagedPolicyReference(scope, appA, Guid.NewGuid()), 1),
                Guid.NewGuid(),
                new ApplicationSecurityModelReference(scope, appA, 1),
                new CapabilityKey("billing", "invoice", "read")));
        }

        [Fact]
        public void Grant_can_preserve_scoped_binding_provenance_without_evaluating_it()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var app = new ApplicationKey("app-a");
            var subject = new SubjectReference(scope, Guid.NewGuid());
            var target = new ResourceScopeReference(tenant, app, Guid.NewGuid());
            var grant = new AssignedCapabilityGrant(subject, tenant, app,
                new GroupReference(tenant, app, Guid.NewGuid()),
                new ManagedPolicyVersionReference(new ManagedPolicyReference(scope, app, Guid.NewGuid()), 1),
                Guid.NewGuid(), new ApplicationSecurityModelReference(scope, app, 1),
                new CapabilityPattern("billing", "invoice", "read"), target, true);

            Assert.Equal(target, grant.TargetScope);
            Assert.True(grant.IncludeDescendants);
        }


        [Fact]
        public void Grant_preserves_shared_managed_policy_version_without_tenantizing_the_policy_definition()
        {
            var scope = Guid.NewGuid();
            var tenant = new TenantReference(scope, Guid.NewGuid());
            var app = new ApplicationKey("app-a");
            var subject = new SubjectReference(scope, Guid.NewGuid());
            var group = new GroupReference(tenant, app, Guid.NewGuid());
            var managedPolicy = new ManagedPolicyVersionReference(
                new ManagedPolicyReference(scope, app, Guid.NewGuid()),
                4);
            var statementId = Guid.NewGuid();
            var model = new ApplicationSecurityModelReference(scope, app, 3);

            var grant = new AssignedCapabilityGrant(
                subject, tenant, app, group, managedPolicy, statementId, model,
                new CapabilityPattern("storage", "object", "read"));

            Assert.Equal(managedPolicy, grant.ManagedPolicyVersion);
            Assert.Equal(tenant, grant.Tenant);
            Assert.Equal(group, grant.Group);
        }

        [Fact]
        public void Assignment_reader_contract_requires_explicit_cancellation_token()
        {
            var method = Assert.Single(typeof(IAssignedCapabilityReader).GetMethods());
            var last = Assert.Single(method.GetParameters().TakeLast(1));
            Assert.Equal(typeof(CancellationToken), last.ParameterType);
            Assert.False(last.IsOptional);
        }
    }
}
