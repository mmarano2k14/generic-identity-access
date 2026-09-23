namespace IdentityAccess.Tests.Mfa.Totp
{
    internal sealed class TotpTestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TotpTestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
