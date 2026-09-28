using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authorization
{
    /// <summary>Projects published managed-policy capabilities currently attached to one tenant group.</summary>
    public interface IGroupCapabilityGrantReader
    {
        Task<IReadOnlyList<GroupCapabilityGrant>> ListAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            CancellationToken cancellationToken);
    }
}
