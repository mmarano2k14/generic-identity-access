using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persistence for statements belonging to managed-policy versions.</summary>
    public interface IManagedPolicyStatementStore
    {
        /// <summary>Adds a managed-policy statement.</summary>
        Task AddAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyStatement statement,
            CancellationToken cancellationToken);

        /// <summary>Removes a statement from an unpublished managed-policy version.</summary>
        Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersionReference policyVersion,
            Guid statementId,
            CancellationToken cancellationToken);

        /// <summary>Lists statements for one managed-policy version.</summary>
        Task<IReadOnlyList<ManagedPolicyStatement>> ListAsync(
            ResolvedDatabaseRoute route,
            ManagedPolicyVersion version,
            CancellationToken cancellationToken);
    }
}
