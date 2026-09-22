using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Persists identity-scope administration policy statements.</summary>
    public interface IIdentityScopeAdministrationPolicyStatementStore
    {
        /// <summary>Lists statements for a policy.</summary>
        Task<IReadOnlyList<IdentityScopeAdministrationPolicyStatement>> ListAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy,
            CancellationToken cancellationToken);

        /// <summary>Adds a policy statement.</summary>
        Task AddAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyStatement statement,
            CancellationToken cancellationToken);

        /// <summary>Removes a policy statement.</summary>
        Task<bool> RemoveAsync(
            ResolvedDatabaseRoute route,
            IdentityScopeAdministrationPolicyReference policy,
            Guid statementId,
            CancellationToken cancellationToken);
    }
}
