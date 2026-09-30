using OrganisationProfile.Domain;

namespace OrganisationProfile.Application.Storage
{
    /// <summary>Persists mutable OrganisationProfile lifecycle state.</summary>
    public interface IOrganisationProfileStore
    {
        /// <summary>Gets one profile by stable technical identity.</summary>
        Task<Domain.OrganisationProfile?> GetAsync(
            OrganisationProfileId organisationProfileId,
            CancellationToken cancellationToken);

        /// <summary>Finds the at-most-one profile attached to an Organization.</summary>
        Task<Domain.OrganisationProfile?> FindByOrganizationAsync(
            OrganizationReference organization,
            CancellationToken cancellationToken);

        /// <summary>Lists profiles inside one Identity Scope and Tenant using bounded paging.</summary>
        Task<IReadOnlyList<Domain.OrganisationProfile>> ListAsync(
            Guid identityScopeId,
            Guid tenantId,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        /// <summary>Creates one profile with durable row version 1.</summary>
        Task<Domain.OrganisationProfile> CreateAsync(
            Domain.OrganisationProfile profile,
            CancellationToken cancellationToken);

        /// <summary>Updates profile lifecycle/template state under optimistic concurrency.</summary>
        Task<Domain.OrganisationProfile?> UpdateAsync(
            Domain.OrganisationProfile profile,
            CancellationToken cancellationToken);
    }
}
