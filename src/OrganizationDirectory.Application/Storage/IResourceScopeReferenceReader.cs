using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Reads the minimum Identity Access ResourceScope state required by Organization Directory.</summary>
    public interface IResourceScopeReferenceReader
    {
        /// <summary>Gets one ResourceScope from the authoritative Identity Access model.</summary>
        Task<ResourceScopeReferenceState?> GetAsync(
            IdentityScopeId identityScopeId,
            TenantId tenantId,
            ApplicationKey application,
            ResourceScopeId resourceScopeId,
            CancellationToken cancellationToken);
    }
}
