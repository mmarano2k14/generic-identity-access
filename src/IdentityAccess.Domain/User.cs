

namespace IdentityAccess.Domain
{

    /// <summary>Immutable domain state. Status changes require a future authorized application operation.</summary>
    public sealed class User
    {
        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }
        /// <summary>Gets the display name.</summary>
        public string DisplayName { get; }
        /// <summary>Gets the status.</summary>
        public UserStatus Status { get; }

        /// <summary>Initializes a new instance of <see cref="User"/>.</summary>
        public User(SubjectReference reference, string displayName, UserStatus status = UserStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(reference);
            Subject = reference;
            DisplayName = ModelGuard.DisplayName(displayName, nameof(displayName));
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
