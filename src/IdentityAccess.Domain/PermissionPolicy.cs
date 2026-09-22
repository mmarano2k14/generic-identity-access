

namespace IdentityAccess.Domain
{

    /// <summary>Mutable policy metadata. Statements and bindings are separate durable structures.</summary>
    public sealed class PermissionPolicy
    {
        /// <summary>Gets the reference.</summary>
        public PermissionPolicyReference Reference { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public PolicyStatus Status { get; }

        /// <summary>Initializes a new instance of <see cref="PermissionPolicy"/>.</summary>
        public PermissionPolicy(PermissionPolicyReference reference, string displayName,
            PolicyStatus status = PolicyStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Reference = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
