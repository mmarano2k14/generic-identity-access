using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the audit contract from accidental inclusion of secret-bearing fields.
    /// </summary>
    public sealed class SecurityAuditArchitectureTests
    {
        /// <summary>Verifies that the audit event contract contains no obvious secret fields.</summary>
        [Fact]
        public void Security_audit_event_does_not_expose_secret_fields()
        {
            var names = typeof(SecurityAuditEvent)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            foreach (var forbidden in new[]
            {
                "Password",
                "PasswordHash",
                "SessionToken",
                "ConnectionString",
                "SecretReference",
                "Payload"
            })
            {
                Assert.DoesNotContain(forbidden, names, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>Verifies that audit categories remain typed.</summary>
        [Fact]
        public void Security_audit_categories_are_typed()
        {
            Assert.True(typeof(SecurityAuditEventType).IsEnum);
            Assert.True(typeof(SecurityAuditOutcome).IsEnum);
            Assert.True(typeof(SecurityAuditReasonCode).IsEnum);
        }
    }
}
