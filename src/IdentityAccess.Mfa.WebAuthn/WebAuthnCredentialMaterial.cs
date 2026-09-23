namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Validated public credential material produced by a WebAuthn registration ceremony.</summary>
    internal sealed class WebAuthnCredentialMaterial
    {
        public byte[] CredentialId { get; }
        public byte[] CosePublicKey { get; }
        public int CoseAlgorithm { get; }
        public byte[] Aaguid { get; }
        public long SignCount { get; }
        public bool BackupEligible { get; }
        public bool BackupState { get; }
        public byte[] UserHandle { get; }

        public WebAuthnCredentialMaterial(
            byte[] credentialId,
            byte[] cosePublicKey,
            int coseAlgorithm,
            byte[] aaguid,
            long signCount,
            bool backupEligible,
            bool backupState,
            byte[] userHandle)
        {
            ArgumentNullException.ThrowIfNull(credentialId);
            ArgumentNullException.ThrowIfNull(cosePublicKey);
            ArgumentNullException.ThrowIfNull(aaguid);
            ArgumentNullException.ThrowIfNull(userHandle);
            if (credentialId.Length is 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(credentialId));
            if (cosePublicKey.Length is 0 or > 4096) throw new ArgumentOutOfRangeException(nameof(cosePublicKey));
            if (aaguid.Length != 16) throw new ArgumentException("AAGUID must contain 16 bytes.", nameof(aaguid));
            if (userHandle.Length != WebAuthnProviderOptions.UserHandleLengthBytes) throw new ArgumentException("WebAuthn user handle has an invalid length.", nameof(userHandle));
            if (signCount is < 0 or > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(signCount));
            if (backupState && !backupEligible) throw new ArgumentException("Backup state requires backup eligibility.", nameof(backupState));

            CredentialId = credentialId.ToArray();
            CosePublicKey = cosePublicKey.ToArray();
            CoseAlgorithm = coseAlgorithm;
            Aaguid = aaguid.ToArray();
            SignCount = signCount;
            BackupEligible = backupEligible;
            BackupState = backupState;
            UserHandle = userHandle.ToArray();
        }
    }
}
