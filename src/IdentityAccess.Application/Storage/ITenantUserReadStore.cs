using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Storage
{
    /// <summary>Reads tenant-constrained user projections without exposing the scope-wide directory.</summary>
    public interface ITenantUserReadStore
    {
        /// <summary>Lists users joined through tenant membership before filtering and pagination.</summary>
        Task<IReadOnlyList<TenantUserReadRecord>> ListAsync(
            ResolvedDatabaseRoute route,
            TenantReference tenant,
            string? search,
            bool activeMembershipsOnly,
            int offset,
            int limit,
            CancellationToken cancellationToken);
    }
}
