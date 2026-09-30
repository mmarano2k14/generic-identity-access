using OrganisationProfile.Domain;

namespace OrganisationProfile.Application
{
    /// <summary>
    /// Narrow integration boundary for validating Organization identity without depending on
    /// Generic Organization Directory implementation assemblies.
    /// </summary>
    public interface IOrganizationReferenceReader
    {
        /// <summary>Gets the referenced Organization state, or null when it does not exist.</summary>
        Task<OrganizationReferenceState?> GetAsync(
            OrganizationReference reference,
            CancellationToken cancellationToken);
    }
}
