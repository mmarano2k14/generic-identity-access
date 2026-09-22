

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Defines configuration options for authentication.</summary>
    public sealed class AuthenticationOptions
    {
        /// <summary>Defines the default session lifetime minutes constant.</summary>
        public const int DefaultSessionLifetimeMinutes = 60;
        /// <summary>Defines the default lockout attempts constant.</summary>
        public const int DefaultLockoutAttempts = 5;
        /// <summary>Defines the default lockout minutes constant.</summary>
        public const int DefaultLockoutMinutes = 15;

        /// <summary>Gets or initializes the session lifetime minutes.</summary>
        public int SessionLifetimeMinutes { get; init; } = DefaultSessionLifetimeMinutes;
        /// <summary>Gets or initializes the lockout attempts.</summary>
        public int LockoutAttempts { get; init; } = DefaultLockoutAttempts;
        /// <summary>Gets or initializes the lockout minutes.</summary>
        public int LockoutMinutes { get; init; } = DefaultLockoutMinutes;

        /// <summary>Validates the current value and throws when it violates the contract.</summary>
        public void Validate()
        {
            if (SessionLifetimeMinutes is < 5 or > 1440)
                throw new ArgumentOutOfRangeException(nameof(SessionLifetimeMinutes));
            if (LockoutAttempts is < 2 or > 20)
                throw new ArgumentOutOfRangeException(nameof(LockoutAttempts));
            if (LockoutMinutes is < 1 or > 1440)
                throw new ArgumentOutOfRangeException(nameof(LockoutMinutes));
        }
    }
}
