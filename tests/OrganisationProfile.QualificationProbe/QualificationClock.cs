using OrganisationProfile.Application;

namespace OrganisationProfile.QualificationProbe
{
    internal sealed class QualificationClock(DateTimeOffset utcNow) : IOrganisationProfileClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
