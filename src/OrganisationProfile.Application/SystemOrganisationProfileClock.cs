namespace OrganisationProfile.Application
{
    /// <summary>Provides UTC system time for hosted OrganisationProfile application services.</summary>
    public sealed class SystemOrganisationProfileClock : IOrganisationProfileClock
    {
        /// <inheritdoc />
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
