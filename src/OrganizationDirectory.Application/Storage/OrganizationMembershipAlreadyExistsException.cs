namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when the same tenant member is already related to the same organization.</summary>
    public sealed class OrganizationMembershipAlreadyExistsException : Exception
    {
        /// <summary>Initializes an organization-membership conflict.</summary>
        public OrganizationMembershipAlreadyExistsException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
