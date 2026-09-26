namespace IdentityAccess.Domain
{
    /// <summary>
    /// Reusable application-managed policy metadata. Tenant-specific grants are represented separately by bindings.
    /// </summary>
    public sealed class ManagedPolicy
    {
        /// <summary>Gets the reference.</summary>
        public ManagedPolicyReference Reference { get; }
        /// <summary>Gets the stable managed policy key.</summary>
        public ManagedPolicyKey Key { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public PolicyStatus Status { get; }
        /// <summary>Gets the published default version when one has been selected.</summary>
        public int? DefaultVersion { get; }

        /// <summary>Initializes a new instance of <see cref="ManagedPolicy"/>.</summary>
        public ManagedPolicy(
            ManagedPolicyReference reference,
            ManagedPolicyKey key,
            string displayName,
            PolicyStatus status = PolicyStatus.Active,
            int? defaultVersion = null)
        {
            ArgumentNullException.ThrowIfNull(reference);
            ArgumentNullException.ThrowIfNull(key);
            if (defaultVersion is <= 0) throw new ArgumentOutOfRangeException(nameof(defaultVersion));
            Reference = reference;
            Key = key;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
            DefaultVersion = defaultVersion;
        }
    }
}
