namespace IdentityAccess.Mfa.Recovery
{
    /// <summary>Validated server-side settings for the recovery-code provider.</summary>
    internal sealed class RecoveryCodeProviderOptions
    {
        internal const string HashAlgorithmName = "SHA256";
        internal const int CodeCharacterLength = 16;
        internal const int CodeEntropyBits = 80;
        internal const int DefaultCodeCount = 10;
        internal const int MinimumCodeCount = 6;
        internal const int MaximumCodeCount = 20;

        public int CodeCount { get; }

        public RecoveryCodeProviderOptions(int codeCount)
        {
            if (codeCount is < MinimumCodeCount or > MaximumCodeCount)
                throw new ArgumentOutOfRangeException(nameof(codeCount));

            CodeCount = codeCount;
        }
    }
}
