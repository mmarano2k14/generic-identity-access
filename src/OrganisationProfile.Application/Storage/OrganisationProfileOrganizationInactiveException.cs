namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when profile mutation requires an active external Organization.</summary>
    public sealed class OrganisationProfileOrganizationInactiveException : Exception
    {
        /// <summary>Initializes an inactive-Organization error.</summary>
        public OrganisationProfileOrganizationInactiveException(string message) : base(message)
        {
        }
    }
}
