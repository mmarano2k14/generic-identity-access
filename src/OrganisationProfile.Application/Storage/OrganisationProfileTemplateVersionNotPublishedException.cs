namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateVersionNotPublishedException : Exception
    {
        public OrganisationProfileTemplateVersionNotPublishedException(
            string message,
            Exception? innerException = null) : base(message, innerException) { }
    }
}
