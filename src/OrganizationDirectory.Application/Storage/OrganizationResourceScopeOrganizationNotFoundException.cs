namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when a ResourceScope link references an Organization that does not exist.</summary>
    public sealed class OrganizationResourceScopeOrganizationNotFoundException : Exception
    {
        /// <summary>Initializes a missing Organization error.</summary>
        public OrganizationResourceScopeOrganizationNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
