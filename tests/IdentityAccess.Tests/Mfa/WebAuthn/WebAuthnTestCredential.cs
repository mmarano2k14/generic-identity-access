using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    internal sealed class WebAuthnTestCredential : IDisposable
    {
        private readonly ECDsa _key;

        public WebAuthnCredentialMaterial Material { get; }

        private WebAuthnTestCredential(ECDsa key, WebAuthnCredentialMaterial material)
        {
            _key = key;
            Material = material;
        }

        public static WebAuthnTestCredential Create(
            Guid identityScopeId,
            Guid userId,
            long signCount = 0,
            bool backupEligible = true,
            bool backupState = true)
        {
            var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = key.ExportParameters(includePrivateParameters: false);

            var coseWriter = new CborWriter();
            coseWriter.WriteStartMap(5);
            coseWriter.WriteInt32(1);
            coseWriter.WriteInt32(2);
            coseWriter.WriteInt32(3);
            coseWriter.WriteInt32(WebAuthnProviderOptions.CoseAlgorithmEs256);
            coseWriter.WriteInt32(-1);
            coseWriter.WriteInt32(1);
            coseWriter.WriteInt32(-2);
            coseWriter.WriteByteString(parameters.Q.X!);
            coseWriter.WriteInt32(-3);
            coseWriter.WriteByteString(parameters.Q.Y!);
            coseWriter.WriteEndMap();

            var userHandle = DeriveUserHandle(identityScopeId, userId);
            var material = new WebAuthnCredentialMaterial(
                RandomNumberGenerator.GetBytes(32),
                coseWriter.Encode(),
                WebAuthnProviderOptions.CoseAlgorithmEs256,
                new byte[16],
                signCount,
                backupEligible,
                backupState,
                userHandle);
            CryptographicOperations.ZeroMemory(userHandle);
            return new WebAuthnTestCredential(key, material);
        }

        public WebAuthnAuthenticationResponse CreateResponse(
            WebAuthnAuthenticationOptions options,
            string origin,
            long signCount,
            bool? backupEligible = null,
            bool? backupState = null,
            bool tamperSignature = false,
            bool includeUserHandle = true)
        {
            var be = backupEligible ?? Material.BackupEligible;
            var bs = backupState ?? Material.BackupState;

            var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.RelyingPartyId));
            var authenticatorData = new byte[37];
            rpIdHash.CopyTo(authenticatorData, 0);
            authenticatorData[32] = 0x01 | 0x04;
            if (be) authenticatorData[32] |= 0x08;
            if (bs) authenticatorData[32] |= 0x10;
            BinaryPrimitives.WriteUInt32BigEndian(authenticatorData.AsSpan(33, 4), checked((uint)signCount));

            var clientData = JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    type = "webauthn.get",
                    challenge = options.Challenge,
                    origin,
                    crossOrigin = false
                });
            var clientDataHash = SHA256.HashData(clientData);
            var signatureBase = new byte[authenticatorData.Length + clientDataHash.Length];
            authenticatorData.CopyTo(signatureBase, 0);
            clientDataHash.CopyTo(signatureBase, authenticatorData.Length);

            var signature = _key.SignData(
                signatureBase,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence);
            if (tamperSignature)
                signature[^1] ^= 0x01;

            try
            {
                return new WebAuthnAuthenticationResponse(
                    WebAuthnBase64Url.Encode(Material.CredentialId),
                    WebAuthnBase64Url.Encode(clientData),
                    WebAuthnBase64Url.Encode(authenticatorData),
                    WebAuthnBase64Url.Encode(signature),
                    includeUserHandle ? WebAuthnBase64Url.Encode(Material.UserHandle) : null);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rpIdHash);
                CryptographicOperations.ZeroMemory(clientDataHash);
                CryptographicOperations.ZeroMemory(signatureBase);
                CryptographicOperations.ZeroMemory(signature);
            }
        }

        public void Dispose() => _key.Dispose();

        private static byte[] DeriveUserHandle(Guid identityScopeId, Guid userId)
        {
            Span<byte> input = stackalloc byte[32];
            identityScopeId.TryWriteBytes(input[..16]);
            userId.TryWriteBytes(input[16..]);
            return SHA256.HashData(input);
        }
    }
}
