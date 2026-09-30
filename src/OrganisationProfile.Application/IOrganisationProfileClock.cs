namespace OrganisationProfile.Application
{
    /// <summary>Application time source used by deterministic OrganisationProfile services.</summary>
    public interface IOrganisationProfileClock
    {
        /// <summary>Gets current UTC time.</summary>
        DateTimeOffset UtcNow { get; }
    }
}
