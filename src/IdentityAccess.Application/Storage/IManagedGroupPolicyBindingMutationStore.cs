using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>
    /// Atomically validates and persists tenant-scoped bindings to shared managed-policy versions.
    /// </summary>
    public interface IManagedGroupPolicyBindingMutationStore
    {
        /// <summary>
        /// Adds the binding only when the group, managed policy, managed-policy version and optional resource scope
        /// are currently valid and active.
        /// </summary>
        Task<bool> AddIfActiveAsync(
            ResolvedDatabaseRoute route,
            ManagedGroupPolicyBinding binding,
            CancellationToken cancellationToken);
    }
}
