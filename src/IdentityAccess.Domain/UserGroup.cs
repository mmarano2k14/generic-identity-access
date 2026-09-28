namespace IdentityAccess.Domain
{
    /// <summary>
    /// Immutable tenant-scoped group state. A reusable template is not a separate security entity;
    /// it is a normal group explicitly marked as reusable.
    /// </summary>
    public sealed class UserGroup
    {
        /// <summary>Gets the reference.</summary>
        public GroupReference Reference { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public GroupStatus Status { get; }
        /// <summary>Gets whether this real group is available as a reusable template.</summary>
        public bool IsTemplate { get; }

        /// <summary>Initializes a normal tenant group.</summary>
        public UserGroup(GroupReference reference, string displayName, GroupStatus status = GroupStatus.Active)
            : this(reference, displayName, status, false)
        {
        }

        /// <summary>Initializes a tenant group with explicit reusable-template state.</summary>
        public UserGroup(
            GroupReference reference,
            string displayName,
            GroupStatus status,
            bool isTemplate)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Reference = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
            IsTemplate = isTemplate;
        }
    }
}
