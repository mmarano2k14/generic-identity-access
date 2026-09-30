namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateVersionAlreadyExistsException : Exception
    {
        public OrganisationProfileTemplateVersionAlreadyExistsException(
            string message,
            Exception? innerException = null) : base(message, innerException) { }
    }
}
