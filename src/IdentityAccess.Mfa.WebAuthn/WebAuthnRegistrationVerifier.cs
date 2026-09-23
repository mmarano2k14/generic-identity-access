using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityAccess.Mfa.WebAuthn
{
    /// <summary>
    /// Validates the initial WebAuthn registration profile: same-origin client data, none
    /// attestation, required user presence and verification, and an ES256/P-256 COSE public key.
    /// </summary>
    internal static class WebAuthnRegistrationVerifier
    {
        private const byte UserPresentFlag = 0x01;
        private const byte UserVerifiedFlag = 0x04;
        private const byte BackupEligibleFlag = 0x08;
        private const byte BackupStateFlag = 0x10;
        private const byte AttestedCredentialDataFlag = 0x40;
        private const byte ExtensionDataFlag = 0x80;
        private const int MaximumClientDataBytes = 64 * 1024;
        private const int MaximumAttestationObjectBytes = 1024 * 1024;
        private const int MinimumAuthenticatorDataBytes = 37 + 16 + 2 + 1;

        internal static WebAuthnRegistrationVerificationResult Verify(
            WebAuthnPendingRegistrationState state,
            WebAuthnRegistrationResponse response,
            WebAuthnProviderOptions options,
            byte[] userHandle)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(userHandle);

            if (!WebAuthnBase64Url.TryDecode(response.ClientDataJson, MaximumClientDataBytes, out var clientDataJson) ||
                !WebAuthnBase64Url.TryDecode(response.AttestationObject, MaximumAttestationObjectBytes, out var attestationObject) ||
                !WebAuthnBase64Url.TryDecode(response.CredentialId, 1023, out var responseCredentialId))
            {
                return WebAuthnRegistrationVerificationResult.InvalidClientData();
            }

            if (!ValidateClientData(clientDataJson, state.ChallengeHash, options))
                return WebAuthnRegistrationVerificationResult.InvalidClientData();

            try
            {
                var authData = ReadNoneAttestationObject(attestationObject);
                if (authData is null || authData.Length < MinimumAuthenticatorDataBytes)
                    return WebAuthnRegistrationVerificationResult.InvalidAttestation();

                var material = ParseAuthenticatorData(authData, responseCredentialId, options, userHandle);
                return material is null
                    ? WebAuthnRegistrationVerificationResult.InvalidAttestation()
                    : WebAuthnRegistrationVerificationResult.Success(material);
            }
            catch (Exception exception) when (
                exception is CborContentException or InvalidOperationException or ArgumentException or CryptographicException)
            {
                return WebAuthnRegistrationVerificationResult.InvalidAttestation();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(clientDataJson);
                CryptographicOperations.ZeroMemory(attestationObject);
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
                    !string.Equals(type.GetString(), "webauthn.create", StringComparison.Ordinal))
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
                    (crossOrigin.ValueKind != JsonValueKind.False &&
                     !(crossOrigin.ValueKind == JsonValueKind.Undefined)))
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

        private static byte[]? ReadNoneAttestationObject(byte[] attestationObject)
        {
            var reader = new CborReader(attestationObject, CborConformanceMode.Strict);
            var mapLength = reader.ReadStartMap();
            if (mapLength is null || mapLength.Value != 3) return null;

            string? format = null;
            byte[]? authData = null;
            var attestationStatementSeen = false;
            var keys = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < mapLength.Value; index++)
            {
                var key = reader.ReadTextString();
                if (!keys.Add(key)) return null;

                switch (key)
                {
                    case "fmt":
                        format = reader.ReadTextString();
                        break;
                    case "authData":
                        authData = reader.ReadByteString();
                        break;
                    case "attStmt":
                    {
                        var attestationLength = reader.ReadStartMap();
                        if (attestationLength is null || attestationLength.Value != 0) return null;
                        reader.ReadEndMap();
                        attestationStatementSeen = true;
                        break;
                    }
                    default:
                        return null;
                }
            }

            reader.ReadEndMap();
            if (reader.BytesRemaining != 0 ||
                !string.Equals(format, "none", StringComparison.Ordinal) ||
                authData is null || !attestationStatementSeen)
            {
                return null;
            }

            return authData;
        }

        private static WebAuthnCredentialMaterial? ParseAuthenticatorData(
            byte[] authData,
            byte[] responseCredentialId,
            WebAuthnProviderOptions options,
            byte[] userHandle)
        {
            var expectedRpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.RelyingPartyId));
            try
            {
                if (!CryptographicOperations.FixedTimeEquals(authData.AsSpan(0, 32), expectedRpIdHash))
                    return null;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expectedRpIdHash);
            }

            var flags = authData[32];
            if ((flags & UserPresentFlag) == 0 ||
                (flags & UserVerifiedFlag) == 0 ||
                (flags & AttestedCredentialDataFlag) == 0 ||
                (flags & ExtensionDataFlag) != 0)
            {
                return null;
            }

            var backupEligible = (flags & BackupEligibleFlag) != 0;
            var backupState = (flags & BackupStateFlag) != 0;
            if (backupState && !backupEligible) return null;

            var signCount = BinaryPrimitives.ReadUInt32BigEndian(authData.AsSpan(33, 4));
            var offset = 37;
            var aaguid = authData.AsSpan(offset, 16).ToArray();
            offset += 16;

            var credentialIdLength = BinaryPrimitives.ReadUInt16BigEndian(authData.AsSpan(offset, 2));
            offset += 2;
            if (credentialIdLength is 0 or > 1023 || authData.Length < offset + credentialIdLength + 1)
                return null;

            var credentialId = authData.AsSpan(offset, credentialIdLength).ToArray();
            offset += credentialIdLength;
            if (!CryptographicOperations.FixedTimeEquals(credentialId, responseCredentialId))
                return null;

            var keyReader = new CborReader(authData.AsMemory(offset), CborConformanceMode.Strict);
            var encodedKey = keyReader.ReadEncodedValue().ToArray();
            if (keyReader.BytesRemaining != 0 || !ValidateEs256PublicKey(encodedKey))
                return null;

            return new WebAuthnCredentialMaterial(
                credentialId,
                encodedKey,
                WebAuthnProviderOptions.CoseAlgorithmEs256,
                aaguid,
                signCount,
                backupEligible,
                backupState,
                userHandle);
        }

        private static bool ValidateEs256PublicKey(byte[] encodedKey)
        {
            var reader = new CborReader(encodedKey, CborConformanceMode.Strict);
            var mapLength = reader.ReadStartMap();
            if (mapLength is null || mapLength.Value < 5 || mapLength.Value > 16) return false;

            int? keyType = null;
            int? algorithm = null;
            int? curve = null;
            byte[]? x = null;
            byte[]? y = null;
            var labels = new HashSet<int>();

            for (var index = 0; index < mapLength.Value; index++)
            {
                var label = reader.ReadInt32();
                if (!labels.Add(label)) return false;

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
                return false;
            }

            using var key = ECDsa.Create();
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
            return key.KeySize == 256;
        }
    }
}
