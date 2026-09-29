namespace OrganizationDirectory.Application
{
    /// <summary>Supplies time to application services without ambient mutable state.</summary>
    public interface IOrganizationClock
    {
        /// <summary>Gets the current UTC time.</summary>
        DateTimeOffset UtcNow { get; }
    }
}
