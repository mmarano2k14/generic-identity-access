using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects fail-closed Organization Directory administration and semantic audit wiring.</summary>
    public sealed class OrganizationAdministrationSecurityTests
    {
        [Fact]
        public void Organization_audit_event_identifiers_are_stable_and_append_only()
        {
            Assert.Equal(70, (int)SecurityAuditEventType.OrganizationCreated);
            Assert.Equal(71, (int)SecurityAuditEventType.OrganizationUpdated);
            Assert.Equal(72, (int)SecurityAuditEventType.OrganizationStatusChanged);
            Assert.Equal(73, (int)SecurityAuditEventType.OrganizationMembershipAdded);
            Assert.Equal(74, (int)SecurityAuditEventType.OrganizationMembershipStatusChanged);
            Assert.Equal(75, (int)SecurityAuditEventType.OrganizationMembershipRemoved);
            Assert.Equal(76, (int)SecurityAuditEventType.OrganizationResourceScopeLinked);
            Assert.Equal(77, (int)SecurityAuditEventType.OrganizationResourceScopeRelinked);
            Assert.Equal(78, (int)SecurityAuditEventType.OrganizationResourceScopeUnlinked);
        }

        [Fact]
        public void Organization_mutation_controllers_are_capability_protected_and_audited()
        {
            var root = FindRepositoryRoot();

            AssertController(
                root,
                "OrganizationsController.cs",
                "IdentityAccessAdministrationCapabilities.Organizations",
                "SecurityAuditEventType.OrganizationCreated");

            AssertController(
                root,
                "OrganizationMembershipsController.cs",
                "IdentityAccessAdministrationCapabilities.OrganizationMemberships",
                "SecurityAuditEventType.OrganizationMembershipAdded");

            AssertController(
                root,
                "OrganizationResourceScopeLinksController.cs",
                "IdentityAccessAdministrationCapabilities.OrganizationScopeLinks",
                "SecurityAuditEventType.OrganizationResourceScopeLinked");
        }

        private static void AssertController(
            string root,
            string file,
            string capability,
            string auditEvent)
        {
            var source = File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "IdentityAccess.Api",
                    "Controllers",
                    file));

            Assert.Contains(
                "RequireAdministrationCapability",
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                capability,
                source,
                StringComparison.Ordinal);

            Assert.Contains(
                auditEvent,
                source,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                "AllowAnonymous",
                source,
                StringComparison.Ordinal);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "IdentityAccess.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException(
                "Repository root could not be located.");
        }
    }
}
