namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when a ResourceScope-link mutation uses a stale row version.</summary>
    public sealed class OrganizationResourceScopeLinkConcurrencyException : Exception
    {
        /// <summary>Initializes a ResourceScope-link concurrency conflict.</summary>
        public OrganizationResourceScopeLinkConcurrencyException(string message) : base(message)
        {
        }
    }
}
