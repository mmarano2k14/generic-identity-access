using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Reads the minimum Identity Access tenant-membership state required by Organization Directory.</summary>
    public interface ITenantMembershipReferenceReader
    {
        /// <summary>Gets tenant-membership state without exposing Identity Access persistence internals.</summary>
        Task<TenantMembershipReferenceState?> GetAsync(
            TenantMembershipReference reference,
            CancellationToken cancellationToken);
    }
}
