using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityAccess.Mfa.WebAuthn;

namespace IdentityAccess.Tests.Mfa.WebAuthn
{
    internal static class WebAuthnTestResponseFactory
    {
        internal static WebAuthnRegistrationResponse Create(
            WebAuthnRegistrationOptions options,
            string origin)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
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
            var cosePublicKey = coseWriter.Encode();

            var credentialId = RandomNumberGenerator.GetBytes(32);
            var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.RelyingPartyId));
            var authData = new byte[37 + 16 + 2 + credentialId.Length + cosePublicKey.Length];
            rpIdHash.CopyTo(authData, 0);
            authData[32] = 0x01 | 0x04 | 0x08 | 0x10 | 0x40;
            BinaryPrimitives.WriteUInt32BigEndian(authData.AsSpan(33, 4), 0);
            var offset = 37;
            new byte[16].CopyTo(authData, offset);
            offset += 16;
            BinaryPrimitives.WriteUInt16BigEndian(authData.AsSpan(offset, 2), (ushort)credentialId.Length);
            offset += 2;
            credentialId.CopyTo(authData, offset);
            offset += credentialId.Length;
            cosePublicKey.CopyTo(authData, offset);

            var attestationWriter = new CborWriter();
            attestationWriter.WriteStartMap(3);
            attestationWriter.WriteTextString("fmt");
            attestationWriter.WriteTextString("none");
            attestationWriter.WriteTextString("authData");
            attestationWriter.WriteByteString(authData);
            attestationWriter.WriteTextString("attStmt");
            attestationWriter.WriteStartMap(0);
            attestationWriter.WriteEndMap();
            attestationWriter.WriteEndMap();

            var clientData = JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    type = "webauthn.create",
                    challenge = options.Challenge,
                    origin,
                    crossOrigin = false
                });

            return new WebAuthnRegistrationResponse(
                WebAuthnBase64Url.Encode(credentialId),
                WebAuthnBase64Url.Encode(clientData),
                WebAuthnBase64Url.Encode(attestationWriter.Encode()));
        }
    }
}
