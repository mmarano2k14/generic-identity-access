namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an organization referenced by an organization membership does not exist.</summary>
    public sealed class OrganizationMembershipReferenceNotFoundException : Exception
    {
        /// <summary>Initializes a missing organization-membership reference error.</summary>
        public OrganizationMembershipReferenceNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
