using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authentication
{

    /// <summary>Server-only credential state. Password hashes are never public API contracts.</summary>
    public sealed class PasswordCredential
    {
        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }
        /// <summary>Gets the login identifier.</summary>
        public LoginIdentifier LoginIdentifier { get; }
        /// <summary>Gets the password hash.</summary>
        public string PasswordHash { get; }
        /// <summary>Gets the failed access count.</summary>
        public int FailedAccessCount { get; }
        /// <summary>Gets the lockout until.</summary>
        public DateTimeOffset? LockoutUntil { get; }

        /// <summary>Initializes a new instance of <see cref="PasswordCredential"/>.</summary>
        public PasswordCredential(SubjectReference subject, LoginIdentifier loginIdentifier, string passwordHash,
            int failedAccessCount = 0, DateTimeOffset? lockoutUntil = null)
        {
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            LoginIdentifier = loginIdentifier ?? throw new ArgumentNullException(nameof(loginIdentifier));
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
            if (failedAccessCount < 0) throw new ArgumentOutOfRangeException(nameof(failedAccessCount));
            PasswordHash = passwordHash;
            FailedAccessCount = failedAccessCount;
            LockoutUntil = lockoutUntil;
        }

        /// <summary>Initializes a new instance of <see cref="PasswordCredential"/>.</summary>
        public override string ToString() => $"PasswordCredential({Subject.UserId:D}) [hash redacted]";
    }
}
