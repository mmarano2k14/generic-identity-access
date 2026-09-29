namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when persisted hierarchy constraints reject an organization mutation.</summary>
    public sealed class OrganizationHierarchyConflictException : Exception
    {
        /// <summary>Initializes a hierarchy conflict.</summary>
        public OrganizationHierarchyConflictException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
