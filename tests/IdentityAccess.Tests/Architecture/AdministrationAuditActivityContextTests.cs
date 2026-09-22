using System.Diagnostics;
using IdentityAccess.Api.Security;
using IdentityAccess.Application.Security;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Verifies trusted administration identity is projected into Activity tags without exposing
    /// the opaque session token.
    /// </summary>
    public sealed class AdministrationAuditActivityContextTests
    {
        /// <summary>Verifies actor provenance tags are derived from the validated context.</summary>
        [Fact]
        public void Trusted_context_populates_safe_audit_activity_tags()
        {
            using var activity = new Activity("audit-test");
            activity.Start();

            var context = new AdministrationRequestContext(
                new SubjectReference(
                    Guid.Parse("35111111-1111-1111-1111-111111111111"),
                    Guid.Parse("35aaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
                Guid.Parse("35bbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                "admin-web",
                new ApplicationKey("app-a"),
                "primary",
                DateTimeOffset.UtcNow.AddMinutes(10));

            AdministrationAuditActivityContext.Apply(context);

            Assert.Equal(
                context.Subject.UserId.ToString("D"),
                activity.GetTagItem(
                    SecurityAuditActivityTagNames.ActorUserId));

            Assert.Equal(
                context.SessionId.ToString("D"),
                activity.GetTagItem(
                    SecurityAuditActivityTagNames.ActorSessionId));

            Assert.Equal(
                context.ClientId,
                activity.GetTagItem(
                    SecurityAuditActivityTagNames.ActorClientId));

            Assert.Null(
                activity.GetTagItem("identity_access.audit.session_token"));
        }
    }
}
