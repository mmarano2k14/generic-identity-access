namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateVersionNotFoundException : Exception
    {
        public OrganisationProfileTemplateVersionNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException) { }
    }
}
