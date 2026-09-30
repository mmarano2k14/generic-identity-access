namespace OrganisationProfile.Application.Storage
{
    /// <summary>Raised when new composition references a disabled template definition.</summary>
    public sealed class OrganisationProfileTemplateInactiveException : Exception
    {
        /// <summary>Initializes an inactive-template error.</summary>
        public OrganisationProfileTemplateInactiveException(string message) : base(message)
        {
        }
    }
}
