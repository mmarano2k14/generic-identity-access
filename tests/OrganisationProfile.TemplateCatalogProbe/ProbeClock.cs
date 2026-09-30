using OrganisationProfile.Application;

namespace OrganisationProfile.TemplateCatalogProbe
{
    internal sealed class ProbeClock(DateTimeOffset utcNow) : IOrganisationProfileClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
