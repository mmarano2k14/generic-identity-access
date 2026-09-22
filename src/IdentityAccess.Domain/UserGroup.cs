

namespace IdentityAccess.Domain
{

    /// <summary>Immutable domain state. Status changes require a future authorized application operation.</summary>
    public sealed class UserGroup
    {
        /// <summary>Gets the reference.</summary>
        public GroupReference Reference { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public GroupStatus Status { get; }

        /// <summary>Initializes a new instance of <see cref="UserGroup"/>.</summary>
        public UserGroup(GroupReference reference, string displayName, GroupStatus status = GroupStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Reference = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
