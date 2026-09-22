using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Authorization
{
    /// <summary>Returns a fixed set of identity-scope grants for authorization tests.</summary>
    internal sealed class IdentityScopeAuthorizationTestGrantReader(
        IReadOnlyList<AssignedIdentityScopeCapabilityGrant> grants)
        : IIdentityScopeAssignedCapabilityReader
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<AssignedIdentityScopeCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            SubjectReference subject,
            ApplicationKey application,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(grants);
        }
    }
}
