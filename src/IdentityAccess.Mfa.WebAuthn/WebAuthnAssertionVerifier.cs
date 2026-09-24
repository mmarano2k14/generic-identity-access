using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>
    /// Verifies the initial WebAuthn assertion profile: exact challenge/origin binding,
    /// required user presence and verification, RP ID binding, ES256 signature validation,
    /// stable backup eligibility, and optional scoped user-handle binding.
    /// </summary>
    internal static class WebAuthnAssertionVerifier
    {
        private const byte UserPresentFlag = 0x01;
        private const byte UserVerifiedFlag = 0x04;
        private const byte BackupEligibleFlag = 0x08;
        private const byte BackupStateFlag = 0x10;
        private const byte AttestedCredentialDataFlag = 0x40;
        private const byte ExtensionDataFlag = 0x80;
        private const int AuthenticatorDataLength = 37;
        private const int MaximumClientDataBytes = 64 * 1024;
        private const int MaximumSignatureBytes = 512;

        internal static WebAuthnAuthenticationVerificationResult Verify(
            WebAuthnAuthenticationChallengeState challengeState,
            WebAuthnCredentialRecord credential,
            WebAuthnAuthenticationResponse response,
            WebAuthnProviderOptions options)
        {
            ArgumentNullException.ThrowIfNull(challengeState);
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(options);

            if (!WebAuthnBase64Url.TryDecode(response.ClientDataJson, MaximumClientDataBytes, out var clientDataJson) ||
                !WebAuthnBase64Url.TryDecode(response.AuthenticatorData, AuthenticatorDataLength, out var authenticatorData) ||
                !WebAuthnBase64Url.TryDecode(response.Signature, MaximumSignatureBytes, out var signature) ||
                !WebAuthnBase64Url.TryDecode(response.CredentialId, 1023, out var responseCredentialId))
            {
                return WebAuthnAuthenticationVerificationResult.InvalidClientData();
            }

            byte[]? userHandle = null;
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(responseCredentialId, credential.CredentialId))
                    return WebAuthnAuthenticationVerificationResult.InvalidAssertion();

                if (response.UserHandle is not null)
                {
                    if (!WebAuthnBase64Url.TryDecode(
                            response.UserHandle,
                            WebAuthnProviderOptions.UserHandleLengthBytes,
                            out userHandle) ||
                        userHandle.Length != WebAuthnProviderOptions.UserHandleLengthBytes ||
                        !CryptographicOperations.FixedTimeEquals(userHandle, credential.UserHandle))
                    {
                        return WebAuthnAuthenticationVerificationResult.InvalidAssertion();
                    }
                }

                if (!ValidateClientData(clientDataJson, challengeState.ChallengeHash, options))
                    return WebAuthnAuthenticationVerificationResult.InvalidClientData();

                if (authenticatorData.Length != AuthenticatorDataLength)
                    return WebAuthnAuthenticationVerificationResult.InvalidAssertion();

                var expectedRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.RelyingPartyId));
                try
                {
                    if (!CryptographicOperations.FixedTimeEquals(authenticatorData.AsSpan(0, 32), expectedRpIdHash))
                        return WebAuthnAuthenticationVerificationResult.InvalidAssertion();
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(expectedRpIdHash);
                }

                var flags = authenticatorData[32];
                if ((flags & UserPresentFlag) == 0 ||
                    (flags & UserVerifiedFlag) == 0 ||
                    (flags & AttestedCredentialDataFlag) != 0 ||
                    (flags & ExtensionDataFlag) != 0)
                {
                    return WebAuthnAuthenticationVerificationResult.InvalidAssertion();
                }

                var backupEligible = (flags & BackupEligibleFlag) != 0;
                var backupState = (flags & BackupStateFlag) != 0;
                if (backupState && !backupEligible)
                    return WebAuthnAuthenticationVerificationResult.InvalidAssertion();
                if (backupEligible != credential.BackupEligible)
                    return WebAuthnAuthenticationVerificationResult.InvalidAssertion();

                var signCount = BinaryPrimitives.ReadUInt32BigEndian(authenticatorData.AsSpan(33, 4));

                using var publicKey = CreateEs256PublicKey(credential.CosePublicKey, credential.CoseAlgorithm);
                if (publicKey is null)
                    return WebAuthnAuthenticationVerificationResult.InvalidAssertion();

                var clientDataHash = SHA256.HashData(clientDataJson);
                var signatureBase = new byte[authenticatorData.Length + clientDataHash.Length];
                try
                {
                    authenticatorData.CopyTo(signatureBase, 0);
                    clientDataHash.CopyTo(signatureBase, authenticatorData.Length);

                    if (!publicKey.VerifyData(
                            signatureBase,
                            signature,
                            HashAlgorithmName.SHA256,
                            DSASignatureFormat.Rfc3279DerSequence))
                    {
                        return WebAuthnAuthenticationVerificationResult.InvalidAssertion();
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(clientDataHash);
                    CryptographicOperations.ZeroMemory(signatureBase);
                }

                return WebAuthnAuthenticationVerificationResult.Success(
                    signCount,
                    backupEligible,
                    backupState);
            }
            catch (Exception exception) when (
                exception is CborContentException or InvalidOperationException or ArgumentException or CryptographicException)
            {
                return WebAuthnAuthenticationVerificationResult.InvalidAssertion();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(clientDataJson);
                CryptographicOperations.ZeroMemory(authenticatorData);
                CryptographicOperations.ZeroMemory(signature);
                CryptographicOperations.ZeroMemory(responseCredentialId);
                if (userHandle is not null)
                    CryptographicOperations.ZeroMemory(userHandle);
            }
        }

        private static bool ValidateClientData(
            byte[] clientDataJson,
            byte[] expectedChallengeHash,
            WebAuthnProviderOptions options)
        {
            try
            {
                using var document = JsonDocument.Parse(clientDataJson);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return false;

                if (!root.TryGetProperty("type", out var type) ||
                    type.ValueKind != JsonValueKind.String ||
                    !string.Equals(type.GetString(), "webauthn.get", StringComparison.Ordinal))
                {
                    return false;
                }

                if (!root.TryGetProperty("challenge", out var challenge) ||
                    challenge.ValueKind != JsonValueKind.String ||
                    !WebAuthnBase64Url.TryDecode(challenge.GetString()!, 128, out var challengeBytes))
                {
                    return false;
                }

                try
                {
                    var actualChallengeHash = SHA256.HashData(challengeBytes);
                    try
                    {
                        if (!CryptographicOperations.FixedTimeEquals(actualChallengeHash, expectedChallengeHash))
                            return false;
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(actualChallengeHash);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(challengeBytes);
                }

                if (!root.TryGetProperty("origin", out var origin) ||
                    origin.ValueKind != JsonValueKind.String ||
                    origin.GetString() is not string originValue ||
                    !options.AllowedOrigins.Contains(originValue))
                {
                    return false;
                }

                if (root.TryGetProperty("crossOrigin", out var crossOrigin) &&
                    crossOrigin.ValueKind != JsonValueKind.False &&
                    crossOrigin.ValueKind != JsonValueKind.Undefined)
                {
                    return false;
                }

                if (root.TryGetProperty("topOrigin", out _))
                    return false;

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static ECDsa? CreateEs256PublicKey(byte[] encodedKey, int coseAlgorithm)
        {
            if (coseAlgorithm != WebAuthnProviderOptions.CoseAlgorithmEs256)
                return null;

            var reader = new CborReader(encodedKey, CborConformanceMode.Strict);
            var mapLength = reader.ReadStartMap();
            if (mapLength is null || mapLength.Value < 5 || mapLength.Value > 16) return null;

            int? keyType = null;
            int? algorithm = null;
            int? curve = null;
            byte[]? x = null;
            byte[]? y = null;
            var labels = new HashSet<int>();

            for (var index = 0; index < mapLength.Value; index++)
            {
                var label = reader.ReadInt32();
                if (!labels.Add(label)) return null;

                switch (label)
                {
                    case 1:
                        keyType = reader.ReadInt32();
                        break;
                    case 3:
                        algorithm = reader.ReadInt32();
                        break;
                    case -1:
                        curve = reader.ReadInt32();
                        break;
                    case -2:
                        x = reader.ReadByteString();
                        break;
                    case -3:
                        y = reader.ReadByteString();
                        break;
                    default:
                        reader.SkipValue();
                        break;
                }
            }

            reader.ReadEndMap();
            if (reader.BytesRemaining != 0 ||
                keyType != 2 ||
                algorithm != WebAuthnProviderOptions.CoseAlgorithmEs256 ||
                curve != 1 ||
                x is null || y is null || x.Length != 32 || y.Length != 32)
            {
                return null;
            }

            var key = ECDsa.Create();
            try
            {
                key.ImportParameters(
                    new ECParameters
                    {
                        Curve = ECCurve.NamedCurves.nistP256,
                        Q = new ECPoint
                        {
                            X = x,
                            Y = y
                        }
                    });

                if (key.KeySize != 256)
                {
                    key.Dispose();
                    return null;
                }

                return key;
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }
    }
}
