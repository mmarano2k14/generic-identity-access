using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{
    public sealed class ManagedPolicyModelTests
    {
        [Fact]
        public void Managed_policy_reference_is_application_scoped_and_has_no_tenant_dimension()
        {
            var reference = new ManagedPolicyReference(Guid.NewGuid(), new ApplicationKey("admin-app"), Guid.NewGuid());

            Assert.DoesNotContain(reference.GetType().GetProperties(), property =>
                property.Name.Contains("Tenant", StringComparison.Ordinal));
            Assert.Equal("admin-app", reference.Application.Value);
        }

        [Fact]
        public void Managed_policy_key_requires_canonical_application_catalog_grammar()
        {
            Assert.Equal("iam-read-only", new ManagedPolicyKey("iam-read-only").Value);
            Assert.Throws<ArgumentException>(() => new ManagedPolicyKey("IAM Read Only"));
        }

        [Fact]
        public void Managed_policy_version_requires_same_identity_scope_and_application_as_security_model()
        {
            var scope = Guid.NewGuid();
            var policy = new ManagedPolicyReference(scope, new ApplicationKey("admin-app"), Guid.NewGuid());
            var version = new ManagedPolicyVersionReference(policy, 1);

            Assert.Throws<ArgumentException>(() => new ManagedPolicyVersion(
                version,
                new ApplicationSecurityModelReference(Guid.NewGuid(), new ApplicationKey("admin-app"), 1)));
            Assert.Throws<ArgumentException>(() => new ManagedPolicyVersion(
                version,
                new ApplicationSecurityModelReference(scope, new ApplicationKey("other-app"), 1)));
        }

        [Fact]
        public void Managed_policy_statement_is_versioned_without_tenant_ownership()
        {
            var scope = Guid.NewGuid();
            var application = new ApplicationKey("admin-app");
            var policy = new ManagedPolicyReference(scope, application, Guid.NewGuid());
            var version = new ManagedPolicyVersionReference(policy, 3);
            var model = new ApplicationSecurityModelReference(scope, application, 7);
            var statement = new ManagedPolicyStatement(
                Guid.NewGuid(),
                version,
                model,
                new CapabilityPattern("identity-access", "user", "read"));

            Assert.Equal(3, statement.PolicyVersion.Version);
            Assert.Equal(7, statement.Model.Version);
            Assert.Equal("identity-access", statement.Pattern.Resource);
        }
    }
}
