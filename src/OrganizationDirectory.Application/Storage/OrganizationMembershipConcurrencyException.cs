namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an organization-membership mutation uses a stale row version.</summary>
    public sealed class OrganizationMembershipConcurrencyException : Exception
    {
        /// <summary>Initializes an organization-membership concurrency conflict.</summary>
        public OrganizationMembershipConcurrencyException(string message) : base(message)
        {
        }
    }
}
