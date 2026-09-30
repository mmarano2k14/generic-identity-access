namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when a requested OrganisationProfile does not exist.</summary>
    public sealed class OrganisationProfileNotFoundException : Exception
    {
        /// <summary>Initializes a missing-profile error.</summary>
        public OrganisationProfileNotFoundException(string message) : base(message)
        {
        }
    }
}
