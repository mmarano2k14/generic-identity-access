namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when semantic composition is changed or resolved for a disabled profile.</summary>
    public sealed class OrganisationProfileInactiveException : Exception
    {
        /// <summary>Initializes an inactive-profile error.</summary>
        public OrganisationProfileInactiveException(string message) : base(message)
        {
        }
    }
}
