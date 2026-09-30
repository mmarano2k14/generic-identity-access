namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when stable OrganisationProfile identity collides with different durable state.</summary>
    public sealed class OrganisationProfileIdentityConflictException : Exception
    {
        /// <summary>Initializes a profile identity conflict.</summary>
        public OrganisationProfileIdentityConflictException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
