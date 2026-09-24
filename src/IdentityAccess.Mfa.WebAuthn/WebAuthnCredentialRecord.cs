using IdentityAccess.Domain;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>Persisted public credential state required to verify WebAuthn assertions.</summary>
    internal sealed class WebAuthnCredentialRecord
    {
        public Guid AuthenticatorId { get; }
        public UserAuthenticatorStatus Status { get; }
        public byte[] CredentialId { get; }
        public byte[] CosePublicKey { get; }
        public int CoseAlgorithm { get; }
        public long SignCount { get; }
        public bool BackupEligible { get; }
        public bool BackupState { get; }
        public byte[] UserHandle { get; }

        public WebAuthnCredentialRecord(
            Guid authenticatorId,
            UserAuthenticatorStatus status,
            byte[] credentialId,
            byte[] cosePublicKey,
            int coseAlgorithm,
            long signCount,
            bool backupEligible,
            bool backupState,
            byte[] userHandle)
        {
            if (authenticatorId == Guid.Empty) throw new ArgumentException("Authenticator identifier is required.", nameof(authenticatorId));
            ArgumentNullException.ThrowIfNull(credentialId);
            ArgumentNullException.ThrowIfNull(cosePublicKey);
            ArgumentNullException.ThrowIfNull(userHandle);
            if (credentialId.Length is 0 or > 1023) throw new ArgumentOutOfRangeException(nameof(credentialId));
            if (cosePublicKey.Length is 0 or > 4096) throw new ArgumentOutOfRangeException(nameof(cosePublicKey));
            if (coseAlgorithm != WebAuthnProviderOptions.CoseAlgorithmEs256) throw new ArgumentOutOfRangeException(nameof(coseAlgorithm));
            if (signCount is < 0 or > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(signCount));
            if (backupState && !backupEligible) throw new ArgumentException("Backup state requires backup eligibility.", nameof(backupState));
            if (userHandle.Length != WebAuthnProviderOptions.UserHandleLengthBytes) throw new ArgumentException("WebAuthn user handle has an invalid length.", nameof(userHandle));

            AuthenticatorId = authenticatorId;
            Status = status;
            CredentialId = credentialId.ToArray();
            CosePublicKey = cosePublicKey.ToArray();
            CoseAlgorithm = coseAlgorithm;
            SignCount = signCount;
            BackupEligible = backupEligible;
            BackupState = backupState;
            UserHandle = userHandle.ToArray();
        }
    }
}
