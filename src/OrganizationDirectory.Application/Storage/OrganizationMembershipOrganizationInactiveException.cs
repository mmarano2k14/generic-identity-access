namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an organization-membership activation targets an inactive organization.</summary>
    public sealed class OrganizationMembershipOrganizationInactiveException : Exception
    {
        /// <summary>Initializes an inactive organization error.</summary>
        public OrganizationMembershipOrganizationInactiveException(string message) : base(message)
        {
        }
    }
}
