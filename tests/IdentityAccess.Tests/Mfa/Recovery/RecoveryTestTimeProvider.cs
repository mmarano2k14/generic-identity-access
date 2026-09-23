namespace IdentityAccess.Tests.Mfa.Recovery
{
    internal sealed class RecoveryTestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public RecoveryTestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
