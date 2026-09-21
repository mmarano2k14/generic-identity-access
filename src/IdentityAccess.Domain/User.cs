namespace IdentityAccess.Domain;

/// <summary>Immutable domain state. Status changes require a future authorized application operation.</summary>
public sealed class User
{
    public SubjectReference Subject { get; }
    public string DisplayName { get; }
    public UserStatus Status { get; }

    public User(SubjectReference reference, string displayName, UserStatus status = UserStatus.Active)
    {
        ArgumentNullException.ThrowIfNull(reference);
        Subject = reference;
        DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
        Status = ModelGuard.DefinedEnum(status, nameof(status));
    }
}
