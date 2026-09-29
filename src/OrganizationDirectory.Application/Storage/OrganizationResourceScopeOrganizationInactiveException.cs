namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when ResourceScope linkage targets an inactive Organization.</summary>
    public sealed class OrganizationResourceScopeOrganizationInactiveException : Exception
    {
        /// <summary>Initializes an inactive Organization error.</summary>
        public OrganizationResourceScopeOrganizationInactiveException(string message) : base(message)
        {
        }
    }
}
