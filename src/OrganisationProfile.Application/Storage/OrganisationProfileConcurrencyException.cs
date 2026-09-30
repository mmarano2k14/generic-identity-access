namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when a profile mutation uses a stale optimistic-concurrency row version.</summary>
    public sealed class OrganisationProfileConcurrencyException : Exception
    {
        /// <summary>Initializes a profile concurrency conflict.</summary>
        public OrganisationProfileConcurrencyException(string message) : base(message)
        {
        }
    }
}
