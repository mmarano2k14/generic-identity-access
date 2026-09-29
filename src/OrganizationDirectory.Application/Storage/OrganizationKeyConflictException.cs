namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when a tenant-local organization key already exists.</summary>
    public sealed class OrganizationKeyConflictException : Exception
    {
        /// <summary>Initializes an organization-key conflict.</summary>
        public OrganizationKeyConflictException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
