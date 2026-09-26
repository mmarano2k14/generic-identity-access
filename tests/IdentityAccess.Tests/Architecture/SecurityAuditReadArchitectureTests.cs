using IdentityAccess.Api.Controllers;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>Protects the bounded read-only security-audit administration surface.</summary>
    public sealed class SecurityAuditReadArchitectureTests
    {
        /// <summary>Verifies that the public response remains categorical and secret-safe.</summary>
        [Fact]
        public void Security_audit_response_does_not_expose_secret_fields()
        {
            var names = typeof(SecurityAuditEventResponse)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            foreach (var forbidden in new[]
            {
                "Password",
                "PasswordHash",
                "SessionToken",
                "AccessToken",
                "RefreshToken",
                "ConnectionString",
                "SecretReference",
                "Payload"
            })
            {
                Assert.DoesNotContain(forbidden, names, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>Verifies that audit administration uses dedicated focused contracts.</summary>
        [Fact]
        public void Security_audit_read_contracts_are_separated_by_responsibility()
        {
            Assert.True(typeof(ISecurityAuditReader).IsInterface);
            Assert.True(typeof(ISecurityAuditAdministrationService).IsInterface);
            Assert.Equal("security-audit", IdentityAccessAdministrationCapabilities.SecurityAudit);
        }

        /// <summary>Verifies that PostgreSQL reads are explicitly scoped by application.</summary>
        [Fact]
        public void PostgreSql_audit_reader_filters_identity_scope_and_application()
        {
            var root = FindRepositoryRoot();
            var source = File.ReadAllText(Path.Combine(
                root,
                "src",
                "IdentityAccess.Infrastructure.PostgreSql",
                "Security",
                "PostgreSqlSecurityAuditReader.cs"));

            Assert.Contains("identity_scope_id = @scope", source, StringComparison.Ordinal);
            Assert.Contains("application_key = @application_key", source, StringComparison.Ordinal);
            Assert.Contains("ORDER BY occurred_at DESC, event_id DESC", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SELECT *", source, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "IdentityAccess.sln"))) return current.FullName;
                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root could not be located.");
        }
    }
}
