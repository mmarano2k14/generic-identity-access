namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when a new or reactivated organization membership targets an inactive tenant membership.</summary>
    public sealed class TenantMembershipInactiveException : Exception
    {
        /// <summary>Initializes an inactive tenant-membership error.</summary>
        public TenantMembershipInactiveException(string message) : base(message)
        {
        }
    }
}
