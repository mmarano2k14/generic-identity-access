

namespace IdentityAccess.Domain
{

    /// <summary>Immutable domain state. Status changes require a future authorized application operation.</summary>
    public sealed class Tenant
    {
        /// <summary>Gets the reference.</summary>
        public TenantReference Reference { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public TenantStatus Status { get; }

        /// <summary>Initializes a new instance of <see cref="Tenant"/>.</summary>
        public Tenant(TenantReference reference, string displayName, TenantStatus status = TenantStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Reference = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
