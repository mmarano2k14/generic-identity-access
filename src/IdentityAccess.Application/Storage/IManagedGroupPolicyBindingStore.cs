using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persistence for tenant-scoped bindings to shared managed-policy versions.</summary>
    public interface IManagedGroupPolicyBindingStore
    {
        /// <summary>Removes an exact managed-policy binding.</summary>
        Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            ManagedGroupPolicyBinding binding,
            CancellationToken cancellationToken);

        /// <summary>Lists managed-policy bindings for one tenant-scoped group.</summary>
        Task<IReadOnlyList<ManagedGroupPolicyBinding>> ListAsync(
            ResolvedDatabaseRoute route,
            GroupReference group,
            CancellationToken cancellationToken);
    }
}
