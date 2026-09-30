namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateConcurrencyException : Exception
    {
        public OrganisationProfileTemplateConcurrencyException(string message) : base(message) { }
    }
}
