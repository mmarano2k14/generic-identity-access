using IdentityAccess.Domain;

namespace IdentityAccess.Tests.Mfa.Recovery
{
    internal sealed class RecoveryTestSet
    {
        public UserAuthenticator Authenticator { get; set; }
        public Dictionary<string, DateTimeOffset?> Consumed { get; }

        public RecoveryTestSet(UserAuthenticator authenticator, IEnumerable<string> hashes)
        {
            ArgumentNullException.ThrowIfNull(authenticator);
            ArgumentNullException.ThrowIfNull(hashes);

            Authenticator = authenticator;
            Consumed = hashes.ToDictionary(hash => hash, _ => (DateTimeOffset?)null, StringComparer.Ordinal);
        }
    }
}
