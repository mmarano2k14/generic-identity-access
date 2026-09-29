namespace IdentityAccess.Application.Storage
{
    /// <summary>Raised when a reusable-group clone contains invalid or incomplete resource-scope mappings.</summary>
    public sealed class GroupTemplateScopeMappingException : InvalidOperationException
    {
        /// <summary>Initializes the exception with a safe validation message.</summary>
        public GroupTemplateScopeMappingException(string message) : base(message)
        {
        }
    }
}
