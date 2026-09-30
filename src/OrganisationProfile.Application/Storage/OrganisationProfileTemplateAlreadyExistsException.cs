namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateAlreadyExistsException : Exception
    {
        public OrganisationProfileTemplateAlreadyExistsException(
            string message,
            Exception? innerException = null) : base(message, innerException) { }
    }
}
