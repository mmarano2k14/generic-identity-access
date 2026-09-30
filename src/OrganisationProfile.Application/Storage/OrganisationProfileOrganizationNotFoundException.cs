namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when a profile references an Organization that does not exist.</summary>
    public sealed class OrganisationProfileOrganizationNotFoundException : Exception
    {
        /// <summary>Initializes a missing Organization reference error.</summary>
        public OrganisationProfileOrganizationNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
