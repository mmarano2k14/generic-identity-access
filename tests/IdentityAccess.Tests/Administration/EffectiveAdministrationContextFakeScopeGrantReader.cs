using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class EffectiveAdministrationContextFakeScopeGrantReader : IIdentityScopeAssignedCapabilityReader
    {
        public List<AssignedIdentityScopeCapabilityGrant> Grants { get; } = [];

        public Task<IReadOnlyList<AssignedIdentityScopeCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            Guid identityScopeId,
            SubjectReference subject,
            ApplicationKey application,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssignedIdentityScopeCapabilityGrant>>(Grants);
    }
}
