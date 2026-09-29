namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an Organization already has a ResourceScope link for the application.</summary>
    public sealed class OrganizationResourceScopeLinkAlreadyExistsException : Exception
    {
        /// <summary>Initializes an Organization ResourceScope link conflict.</summary>
        public OrganizationResourceScopeLinkAlreadyExistsException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
