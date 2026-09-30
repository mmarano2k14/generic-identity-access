namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateNotFoundException : Exception
    {
        public OrganisationProfileTemplateNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException) { }
    }
}
