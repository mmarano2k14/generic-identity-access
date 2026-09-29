namespace OrganizationDirectory.Application
{
    /// <summary>Default system-clock implementation.</summary>
    public sealed class SystemOrganizationClock : IOrganizationClock
    {
        /// <inheritdoc />
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
