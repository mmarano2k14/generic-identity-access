namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when a stale row version attempts to mutate durable organization state.</summary>
    public sealed class OrganizationConcurrencyException : Exception
    {
        /// <summary>Initializes a concurrency exception.</summary>
        public OrganizationConcurrencyException(string message) : base(message)
        {
        }
    }
}
