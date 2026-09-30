using OrganisationProfile.Application;

namespace OrganisationProfile.CompositionProbe
{
    internal sealed class ProbeClock(DateTimeOffset utcNow) : IOrganisationProfileClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
