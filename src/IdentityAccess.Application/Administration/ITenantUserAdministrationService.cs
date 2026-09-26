using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;

namespace IdentityAccess.Application.Administration
{
    /// <summary>Defines tenant-constrained user reads for administration surfaces.</summary>
    public interface ITenantUserAdministrationService
    {
        /// <summary>Lists user projections constrained by an explicit tenant before filtering and paging.</summary>
        Task<IReadOnlyList<TenantUserReadRecord>> ListAsync(
            Guid identityScopeId,
            ApplicationKey application,
            Guid tenantId,
            string? search,
            bool activeMembershipsOnly,
            int offset,
            int limit,
            CancellationToken cancellationToken);
    }
}
