using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persists identity-scope administration group-policy bindings.</summary>
    public interface IIdentityScopeAdministrationBindingStore
    {
        /// <summary>Lists bindings for a group.</summary>
        Task<IReadOnlyList<IdentityScopeAdministrationGroupPolicyBinding>> ListAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupReference group,
            CancellationToken cancellationToken);

        /// <summary>Adds a binding atomically only when group and policy are active.</summary>
        Task<IdentityScopeAdministrationGroupPolicyBinding?> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupPolicyBinding binding,
            CancellationToken cancellationToken);

        /// <summary>Removes a binding.</summary>
        Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationGroupPolicyBinding binding,
            CancellationToken cancellationToken);
    }
}
