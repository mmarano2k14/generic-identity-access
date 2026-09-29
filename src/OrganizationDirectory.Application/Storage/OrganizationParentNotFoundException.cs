namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when an organization mutation references a parent that does not exist.</summary>
    public sealed class OrganizationParentNotFoundException : Exception
    {
        /// <summary>Initializes a missing-parent exception.</summary>
        public OrganizationParentNotFoundException(string message) : base(message)
        {
        }
    }
}
