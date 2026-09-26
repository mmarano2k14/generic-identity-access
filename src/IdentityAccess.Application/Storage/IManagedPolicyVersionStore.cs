using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persistence for versioned managed-policy definitions.</summary>
    public interface IManagedPolicyVersionStore
    {
        /// <summary>Creates one managed-policy version.</summary>
        Task CreateAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersion version,
            CancellationToken cancellationToken);

        /// <summary>Publishes one managed-policy version and freezes its statements.</summary>
        Task<ManagedPolicyVersion?> PublishAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersionReference reference,
            CancellationToken cancellationToken);

        /// <summary>Gets one managed-policy version.</summary>
        Task<ManagedPolicyVersion?> GetAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersionReference reference,
            CancellationToken cancellationToken);

        /// <summary>Lists managed-policy versions in ascending version order.</summary>
        Task<IReadOnlyList<ManagedPolicyVersion>> ListAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyReference policy,
            CancellationToken cancellationToken);
    }
}
