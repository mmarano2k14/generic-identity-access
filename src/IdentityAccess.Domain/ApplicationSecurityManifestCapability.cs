namespace IdentityAccess.Domain
{
    /// <summary>One concrete capability declared by an application security manifest.</summary>
    public sealed class ApplicationSecurityManifestCapability
    {
        /// <summary>Gets the concrete capability key.</summary>
        public CapabilityKey Key { get; }

        /// <summary>Gets the administrator-facing display name.</summary>
        public string DisplayName { get; }

        /// <summary>Initializes a manifest capability.</summary>
        public ApplicationSecurityManifestCapability(CapabilityKey key, string displayName)
        {
            ArgumentNullException.ThrowIfNull(key);
            Key = key;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
        }
    }
}
