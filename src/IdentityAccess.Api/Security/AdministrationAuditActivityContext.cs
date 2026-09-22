using System.Diagnostics;
using IdentityAccess.Application.Security;

namespace IdentityAccess.Api.Security
{
    /// <summary>
    /// Projects a validated administration context into the current request activity so
    /// infrastructure can attach safe actor provenance to transactional audit records.
    /// </summary>
    internal static class AdministrationAuditActivityContext
    {
        /// <summary>Applies trusted actor identifiers to the current activity when one exists.</summary>
        public static void Apply(
            AdministrationRequestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var activity = Activity.Current;

            if (activity is null)
            {
                return;
            }

            activity.SetTag(
                SecurityAuditActivityTagNames.ActorIdentityScopeId,
                context.Subject.IdentityScopeId.ToString("D"));

            activity.SetTag(
                SecurityAuditActivityTagNames.ActorUserId,
                context.Subject.UserId.ToString("D"));

            activity.SetTag(
                SecurityAuditActivityTagNames.ActorSessionId,
                context.SessionId.ToString("D"));

            activity.SetTag(
                SecurityAuditActivityTagNames.ActorClientId,
                context.ClientId);

            activity.SetTag(
                SecurityAuditActivityTagNames.ActorApplicationKey,
                context.Application.Value);

            activity.SetTag(
                SecurityAuditActivityTagNames.AuthenticationContextKey,
                context.AuthenticationContextKey);
        }
    }
}
