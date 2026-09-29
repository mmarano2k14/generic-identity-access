namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an Identity Access tenant membership referenced by Organization Directory does not exist.</summary>
    public sealed class TenantMembershipReferenceNotFoundException : Exception
    {
        /// <summary>Initializes a missing tenant-membership reference error.</summary>
        public TenantMembershipReferenceNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
