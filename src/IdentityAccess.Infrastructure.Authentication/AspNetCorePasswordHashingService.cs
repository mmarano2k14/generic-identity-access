using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Identity;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Implements password hashing and verification through the ASP.NET Core password hasher
    /// without exposing hashing implementation details to the application layer.
    /// </summary>
    internal sealed class AspNetCorePasswordHashingService : IPasswordHashingService
    {
        private readonly PasswordHasher<string> hasher = new();
        private readonly string dummyHash;

        /// <summary>Initializes a new password hashing service and its unknown-subject timing hash.</summary>
        public AspNetCorePasswordHashingService()
        {
            dummyHash = hasher.HashPassword("unknown-subject", Guid.NewGuid().ToString("N"));
        }

        /// <inheritdoc />
        public string Hash(SubjectReference subject, string password)
        {
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(password);

            return hasher.HashPassword(SubjectKey(subject), password);
        }

        /// <inheritdoc />
        public PasswordHashVerification Verify(
            SubjectReference subject,
            string passwordHash,
            string providedPassword)
        {
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
            ArgumentNullException.ThrowIfNull(providedPassword);

            return hasher.VerifyHashedPassword(
                SubjectKey(subject),
                passwordHash,
                providedPassword) switch
            {
                PasswordVerificationResult.Success => PasswordHashVerification.Success,
                PasswordVerificationResult.SuccessRehashNeeded =>
                    PasswordHashVerification.SuccessRehashNeeded,
                _ => PasswordHashVerification.Failed
            };
        }

        /// <inheritdoc />
        public void ConsumeUnknownCredential(string providedPassword)
        {
            ArgumentNullException.ThrowIfNull(providedPassword);
            _ = hasher.VerifyHashedPassword(
                "unknown-subject",
                dummyHash,
                providedPassword);
        }

        private static string SubjectKey(SubjectReference subject) =>
            $"{subject.IdentityScopeId:D}:{subject.UserId:D}";
    }
}
