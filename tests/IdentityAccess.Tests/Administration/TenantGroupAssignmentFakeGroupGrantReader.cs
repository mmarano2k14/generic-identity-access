using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Administration
{
    internal sealed class TenantGroupAssignmentFakeGroupGrantReader(IReadOnlyList<GroupCapabilityGrant> grants) : IGroupCapabilityGrantReader
    {
        public int CallCount { get; private set; }
        public GroupReference? LastGroup { get; private set; }

        public Task<IReadOnlyList<GroupCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastGroup = group;
            return Task.FromResult(grants);
        }
    }
}
