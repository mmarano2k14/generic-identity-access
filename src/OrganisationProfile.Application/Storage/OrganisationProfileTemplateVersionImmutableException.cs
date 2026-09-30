namespace OrganisationProfile.Application.Storage
{
    public sealed class OrganisationProfileTemplateVersionImmutableException : Exception
    {
        public OrganisationProfileTemplateVersionImmutableException(
            string message,
            Exception? innerException = null) : base(message, innerException) { }
    }
}
