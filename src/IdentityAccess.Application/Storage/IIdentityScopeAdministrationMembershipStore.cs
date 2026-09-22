using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persists identity-scope administration group memberships.</summary>
    public interface IIdentityScopeAdministrationMembershipStore
    {
        /// <summary>Lists members.</summary>
        Task<IReadOnlyList<IdentityScopeAdministrationGroupMembership>> ListAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group,
            CancellationToken cancellationToken);

        /// <summary>Adds a member atomically only when both group and user are active.</summary>
        Task<IdentityScopeAdministrationGroupMembership?> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupMembership membership,
            CancellationToken cancellationToken);

        /// <summary>Removes a member.</summary>
        Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupMembership membership,
            CancellationToken cancellationToken);
    }
}
