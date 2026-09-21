namespace IdentityAccess.Domain;

/// <summary>Immutable domain state. Status changes require a future authorized application operation.</summary>
public sealed class UserGroup
{
    public GroupReference Reference { get; }
    public string DisplayName { get; }
    public GroupStatus Status { get; }

    public UserGroup(GroupReference reference, string displayName, GroupStatus status = GroupStatus.Active)
    {
        ArgumentNullException.ThrowIfNull(reference);
        Reference = reference;
        DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
        Status = ModelGuard.DefinedEnum(status, nameof(status));
    }
}
