using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authorization
{
    /// <summary>
    /// Projects current identity-scope administration capability grants for one subject and
    /// application. It does not evaluate wildcard semantics.
    /// </summary>
    public interface IIdentityScopeAssignedCapabilityReader
    {
        /// <summary>
        /// Lists effective scope-administration grants for the subject from active groups, policies,
        /// and statements.
        /// </summary>
        Task<IReadOnlyList<AssignedIdentityScopeCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            SubjectReference subject,
            ApplicationKey application,
            CancellationToken cancellationToken);
    }
}
