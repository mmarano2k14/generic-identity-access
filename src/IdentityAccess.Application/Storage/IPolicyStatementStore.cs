using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{

    /// <summary>Persistence for immutable policy-to-capability statements.</summary>
    public interface IPolicyStatementStore
    {
        /// <summary>Adds a policy statement record to the resolved database route.</summary>
        Task AddAsync(ResolvedDatabaseRoute route, PolicyStatement statement, CancellationToken cancellationToken);

        /// <summary>Removes a policy statement record from the resolved database route.</summary>
        Task<bool> RemoveAsync(ResolvedDatabaseRoute route, PermissionPolicyReference policy, Guid statementId,
            CancellationToken cancellationToken);

        /// <summary>Lists policy statement records for the supplied scope.</summary>
        Task<IReadOnlyList<PolicyStatement>> ListAsync(ResolvedDatabaseRoute route,
            PermissionPolicyReference policy, CancellationToken cancellationToken);
    }
}
