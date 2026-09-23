namespace IdentityAccess.Domain
{
    /// <summary>Defines whether additional authentication factors are disabled, optional, or required.</summary>
    public enum MfaPolicyMode
    {
        /// <summary>No additional authentication factor is required or enrollable by policy.</summary>
        Disabled = 1,
        /// <summary>Additional authentication factors may be enrolled and used.</summary>
        Optional = 2,
        /// <summary>An additional authentication factor is required by policy.</summary>
        Required = 3
    }
}
