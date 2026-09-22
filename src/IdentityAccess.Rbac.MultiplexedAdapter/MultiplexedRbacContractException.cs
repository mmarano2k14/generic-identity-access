namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>Represents a safe adapter-contract mismatch against the external RBAC distribution.</summary>
    internal sealed class MultiplexedRbacContractException : Exception
    {
        /// <summary>Initializes a contract mismatch with a safe diagnostic message.</summary>
        public MultiplexedRbacContractException(string message)
            : base(message)
        {
        }
    }
}
