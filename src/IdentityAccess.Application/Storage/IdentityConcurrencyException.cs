

namespace IdentityAccess.Application.Storage
{

    /// <summary>Raised when a write no longer matches the row version previously observed by the caller.</summary>
    public sealed class IdentityConcurrencyException : Exception
    {
        /// <summary>Represents was.</summary>
        public IdentityConcurrencyException() : base("The identity record was modified by another operation.") { }
    }
}
