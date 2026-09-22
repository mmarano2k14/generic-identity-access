namespace IdentityAccess.Domain
{
    /// <summary>Mutable metadata for an identity-scope administration group.</summary>
    public sealed class IdentityScopeAdministrationGroup
    {
        /// <summary>Gets the group reference.</summary>
        public IdentityScopeAdministrationGroupReference Reference { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public GroupStatus Status { get; }

        /// <summary>Initializes a scope-administration group.</summary>
        public IdentityScopeAdministrationGroup(
            IdentityScopeAdministrationGroupReference reference,
            string displayName,
            GroupStatus status = GroupStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Reference = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
