using OrganizationDirectory.Application;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>Deterministic application clock for organization tests.</summary>
    internal sealed class TestOrganizationClock(DateTimeOffset utcNow) : IOrganizationClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }
}
