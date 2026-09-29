namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when a new or changed Organization link targets an inactive ResourceScope.</summary>
    public sealed class OrganizationResourceScopeInactiveException : Exception
    {
        /// <summary>Initializes an inactive ResourceScope error.</summary>
        public OrganizationResourceScopeInactiveException(string message) : base(message)
        {
        }
    }
}
