using IdentityAccess.Api.Security;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects the administration trust boundary from raw credential and caller-asserted target
    /// data.
    /// </summary>
    public sealed class AdministrationContextArchitectureTests
    {
        /// <summary>
        /// Verifies that trusted administration context contains authenticated identity provenance
        /// but no raw session token, tenant target, or permission claim.
        /// </summary>
        [Fact]
        public void Administration_context_contains_only_trusted_authentication_identity()
        {
            var propertyNames = typeof(AdministrationRequestContext)
                .GetProperties()
                .Select(property => property.Name)
                .ToArray();

            Assert.Contains("Subject", propertyNames);
            Assert.Contains("SessionId", propertyNames);
            Assert.Contains("ClientId", propertyNames);
            Assert.Contains("Application", propertyNames);
            Assert.Contains("AuthenticationContextKey", propertyNames);
            Assert.Contains("ExpiresAt", propertyNames);

            Assert.DoesNotContain("SessionToken", propertyNames);
            Assert.DoesNotContain("TenantId", propertyNames);
            Assert.DoesNotContain("ResourceScopeId", propertyNames);
            Assert.DoesNotContain("Capabilities", propertyNames);
            Assert.DoesNotContain("Permissions", propertyNames);
        }

        /// <summary>
        /// Verifies that local session validation returns a server-authenticated context instead of
        /// loosely related scalar identifiers.
        /// </summary>
        [Fact]
        public void Session_validation_uses_authenticated_session_context()
        {
            var contextProperty = typeof(SessionValidationResult)
                .GetProperty(nameof(SessionValidationResult.Context));

            Assert.NotNull(contextProperty);
            Assert.Equal(
                typeof(AuthenticatedSessionContext),
                contextProperty.PropertyType);
        }
    }
}
