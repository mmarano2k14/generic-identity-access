namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    internal sealed class WebAuthnTestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public WebAuthnTestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
