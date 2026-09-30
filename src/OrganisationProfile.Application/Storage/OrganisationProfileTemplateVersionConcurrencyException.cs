namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateVersionConcurrencyException : Exception
    {
        public OrganisationProfileTemplateVersionConcurrencyException(string message) : base(message) { }
    }
}
