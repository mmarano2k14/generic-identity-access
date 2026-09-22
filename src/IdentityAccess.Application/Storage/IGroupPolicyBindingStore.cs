using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Persistence for exact group-to-policy bindings. A stored binding is not an authorization decision.</summary>
    public interface IGroupPolicyBindingStore
    {
        /// <summary>Adds a group policy binding record to the resolved database route.</summary>
        Task AddAsync(ResolvedDatabaseRoute route, GroupPolicyBinding binding, CancellationToken cancellationToken);

        /// <summary>Removes a group policy binding record from the resolved database route.</summary>
        Task<bool> RemoveAsync(ResolvedDatabaseRoute route, GroupPolicyBinding binding, CancellationToken cancellationToken);

        /// <summary>Lists group policy binding records for the supplied scope.</summary>
        Task<IReadOnlyList<GroupPolicyBinding>> ListAsync(ResolvedDatabaseRoute route, GroupReference group,
            CancellationToken cancellationToken);
    }
}
