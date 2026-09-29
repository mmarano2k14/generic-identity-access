namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when the same Identity Access ResourceScope is already linked to another Organization.</summary>
    public sealed class OrganizationResourceScopeAlreadyLinkedException : Exception
    {
        /// <summary>Initializes a ResourceScope uniqueness conflict.</summary>
        public OrganizationResourceScopeAlreadyLinkedException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
