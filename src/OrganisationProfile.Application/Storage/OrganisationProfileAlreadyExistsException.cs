namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when an Organization already owns its single allowed OrganisationProfile.</summary>
    public sealed class OrganisationProfileAlreadyExistsException : Exception
    {
        /// <summary>Initializes an Organization/profile uniqueness conflict.</summary>
        public OrganisationProfileAlreadyExistsException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
