namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an organization stable identity already exists.</summary>
    public sealed class OrganizationIdentityConflictException : Exception
    {
        /// <summary>Initializes an organization-identity conflict.</summary>
        public OrganizationIdentityConflictException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
