namespace OrganizationDirectory.Application.Storage
{
    /// <summary>Raised when the referenced Identity Access ResourceScope does not exist in the expected boundary.</summary>
    public sealed class OrganizationResourceScopeReferenceNotFoundException : Exception
    {
        /// <summary>Initializes a missing ResourceScope reference error.</summary>
        public OrganizationResourceScopeReferenceNotFoundException(
            string message,
            Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
