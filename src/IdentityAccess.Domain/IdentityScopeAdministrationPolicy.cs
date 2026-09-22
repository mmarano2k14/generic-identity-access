namespace IdentityAccess.Domain
{
    /// <summary>Mutable metadata for an identity-scope administration policy.</summary>
    public sealed class IdentityScopeAdministrationPolicy
    {
        /// <summary>Gets the policy reference.</summary>
        public IdentityScopeAdministrationPolicyReference Reference { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public PolicyStatus Status { get; }

        /// <summary>Initializes a scope-administration policy.</summary>
        public IdentityScopeAdministrationPolicy(
            IdentityScopeAdministrationPolicyReference reference,
            string displayName,
            PolicyStatus status = PolicyStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Reference = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
