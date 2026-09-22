

namespace IdentityAccess.Domain
{

    /// <summary>A capability declared by a versioned application security model.</summary>
    public sealed class ApplicationCapability
    {
        /// <summary>Gets the model.</summary>
        public ApplicationSecurityModelReference Model { get; }
        /// <summary>Gets the key.</summary>
        public CapabilityKey Key { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }

        /// <summary>Initializes a new instance of <see cref="ApplicationCapability"/>.</summary>
        public ApplicationCapability(ApplicationSecurityModelReference model, CapabilityKey key, string displayName)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(key);
            Model = model;
            Key = key;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
        }
    }
}
