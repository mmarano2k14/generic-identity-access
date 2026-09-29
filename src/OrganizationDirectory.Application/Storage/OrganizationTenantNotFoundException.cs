namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when durable organization state references a tenant that does not exist.</summary>
    public sealed class OrganizationTenantNotFoundException : Exception
    {
        /// <summary>Initializes a missing-tenant exception.</summary>
        public OrganizationTenantNotFoundException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
