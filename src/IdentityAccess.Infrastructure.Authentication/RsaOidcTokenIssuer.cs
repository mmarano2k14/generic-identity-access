using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityAccess.Application.Authentication;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Loads a process-pinned RSA signing-key ring, signs with one active private key, and publishes
    /// all configured public keys for RS256 validation continuity.
    /// </summary>
    internal sealed class RsaOidcTokenIssuer :
        IOidcTokenIssuer,
        IDisposable
    {
        private const int MaximumPublishedKeyCount = 16;

        private readonly RSA activeRsa;
        private readonly OidcOptions options;
        private readonly string activeKeyId;
        private readonly object signingLock = new();

        /// <summary>Initializes and validates the process-pinned OIDC RSA signing-key ring.</summary>
        public RsaOidcTokenIssuer(
            OidcOptions options,
            string activeSigningKeyId,
            IReadOnlyList<RsaOidcSigningKeyOptions> signingKeys)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(signingKeys);

            this.options =
                options.Validate();

            ValidateKeyId(
                activeSigningKeyId,
                "OIDC active signing key id is invalid.");

            if (signingKeys.Count is < 1 or > MaximumPublishedKeyCount)
            {
                throw new InvalidOperationException(
                    $"OIDC signing-key ring must contain between 1 and {MaximumPublishedKeyCount} keys.");
            }

            var keyIds =
                new HashSet<string>(
                    StringComparer.Ordinal);

            var publishedKeys =
                new List<OidcJsonWebKey>(
                    signingKeys.Count);

            RSA? loadedActiveRsa = null;

            try
            {
                foreach (var signingKey in signingKeys)
                {
                    ArgumentNullException.ThrowIfNull(signingKey);

                    ValidateKeyId(
                        signingKey.KeyId,
                        "OIDC signing key id is invalid.");

                    ArgumentException.ThrowIfNullOrWhiteSpace(
                        signingKey.KeyPemPath);

                    if (!keyIds.Add(signingKey.KeyId))
                    {
                        throw new InvalidOperationException(
                            "OIDC signing-key ids must be unique.");
                    }

                    if (!File.Exists(signingKey.KeyPemPath))
                    {
                        throw new InvalidOperationException(
                            $"OIDC signing key file for '{signingKey.KeyId}' is unavailable.");
                    }

                    var rsa =
                        RSA.Create();

                    var retainForSigning = false;

                    try
                    {
                        var pem =
                            File.ReadAllText(
                                signingKey.KeyPemPath);

                        try
                        {
                            rsa.ImportFromPem(pem);
                        }
                        catch (Exception error) when (
                            error is ArgumentException or
                                CryptographicException)
                        {
                            throw new InvalidOperationException(
                                $"OIDC signing key file for '{signingKey.KeyId}' does not contain a supported RSA key.",
                                error);
                        }

                        if (rsa.KeySize < 2048)
                        {
                            throw new InvalidOperationException(
                                "OIDC RSA signing keys must be at least 2048 bits.");
                        }

                        var publicParameters =
                            rsa.ExportParameters(
                                includePrivateParameters: false);

                        var publicKey =
                            new OidcJsonWebKey(
                                "RSA",
                                signingKey.KeyId,
                                "sig",
                                "RS256",
                                OidcBase64Url.Encode(
                                    publicParameters.Modulus ??
                                        throw new InvalidOperationException(
                                            "OIDC RSA modulus is unavailable.")),
                                OidcBase64Url.Encode(
                                    publicParameters.Exponent ??
                                        throw new InvalidOperationException(
                                            "OIDC RSA exponent is unavailable.")));

                        publishedKeys.Add(publicKey);

                        if (string.Equals(
                                signingKey.KeyId,
                                activeSigningKeyId,
                                StringComparison.Ordinal))
                        {
                            try
                            {
                                _ = rsa.ExportParameters(
                                    includePrivateParameters: true);
                            }
                            catch (Exception error) when (
                                error is CryptographicException or
                                    NotSupportedException)
                            {
                                throw new InvalidOperationException(
                                    "The active OIDC signing key must contain private RSA key material.",
                                    error);
                            }

                            loadedActiveRsa =
                                rsa;

                            retainForSigning =
                                true;
                        }
                    }
                    finally
                    {
                        if (!retainForSigning)
                        {
                            rsa.Dispose();
                        }
                    }
                }

                if (loadedActiveRsa is null)
                {
                    throw new InvalidOperationException(
                        "OIDC active signing key id does not identify a configured signing key.");
                }

                var activePublicKey =
                    publishedKeys.Single(
                        key => string.Equals(
                            key.KeyId,
                            activeSigningKeyId,
                            StringComparison.Ordinal));

                SigningKey =
                    activePublicKey;

                SigningKeys =
                    publishedKeys
                        .OrderByDescending(
                            key => string.Equals(
                                key.KeyId,
                                activeSigningKeyId,
                                StringComparison.Ordinal))
                        .ToArray();

                activeRsa =
                    loadedActiveRsa;

                activeKeyId =
                    activeSigningKeyId;
            }
            catch
            {
                loadedActiveRsa?.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public OidcJsonWebKey SigningKey { get; }

        /// <inheritdoc />
        public IReadOnlyList<OidcJsonWebKey> SigningKeys { get; }

        /// <inheritdoc />
        public OidcIssuedTokens Issue(
            OidcTokenIssueRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Subject);
            ArgumentNullException.ThrowIfNull(request.Application);

            if (request.SessionId == Guid.Empty)
                throw new ArgumentException(
                    "A session id is required.",
                    nameof(request));

            ArgumentException.ThrowIfNullOrWhiteSpace(
                request.ClientId);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                request.Scope);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                request.Nonce);

            if (request.AuthenticatedAt > request.IssuedAt)
            {
                throw new ArgumentException(
                    "Authentication time must not be after token issuance.",
                    nameof(request));
            }

            var access =
                IssueAccessToken(
                    new OidcAccessTokenIssueRequest(
                        request.Subject,
                        request.SessionId,
                        request.ClientId,
                        request.Application,
                        request.Scope,
                        request.IssuedAt));

            var idExpiresAt =
                request.IssuedAt.AddMinutes(
                    options.IdTokenLifetimeMinutes);

            var subject =
                SubjectValue(
                    request.Subject);

            var idPayload =
                new Dictionary<string, object>
                {
                    ["iss"] = options.CanonicalIssuer,
                    ["sub"] = subject,
                    ["aud"] = request.ClientId,
                    ["exp"] = idExpiresAt.ToUnixTimeSeconds(),
                    ["iat"] = request.IssuedAt.ToUnixTimeSeconds(),
                    ["auth_time"] = request.AuthenticatedAt.ToUnixTimeSeconds(),
                    ["nonce"] = request.Nonce,
                    ["sid"] = request.SessionId.ToString("D"),
                    ["at_hash"] = AccessTokenHash(access.AccessToken)
                };

            var idToken =
                Sign(
                    idPayload);

            return new OidcIssuedTokens(
                access.AccessToken,
                idToken,
                access.ExpiresIn);
        }

        /// <inheritdoc />
        public OidcIssuedAccessToken IssueAccessToken(
            OidcAccessTokenIssueRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Subject);
            ArgumentNullException.ThrowIfNull(request.Application);

            if (request.SessionId == Guid.Empty)
                throw new ArgumentException(
                    "A session id is required.",
                    nameof(request));

            ArgumentException.ThrowIfNullOrWhiteSpace(
                request.ClientId);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                request.Scope);

            var accessExpiresAt =
                request.IssuedAt.AddMinutes(
                    options.AccessTokenLifetimeMinutes);

            var accessPayload =
                new Dictionary<string, object>
                {
                    ["iss"] = options.CanonicalIssuer,
                    ["sub"] = SubjectValue(request.Subject),
                    ["aud"] = options.AccessTokenAudience,
                    ["exp"] = accessExpiresAt.ToUnixTimeSeconds(),
                    ["iat"] = request.IssuedAt.ToUnixTimeSeconds(),
                    ["jti"] = Guid.NewGuid().ToString("N"),
                    ["client_id"] = request.ClientId,
                    ["scope"] = request.Scope,
                    ["sid"] = request.SessionId.ToString("D"),
                    ["identity_scope_id"] = request.Subject.IdentityScopeId.ToString("D"),
                    ["application_key"] = request.Application.Value
                };

            return new OidcIssuedAccessToken(
                Sign(accessPayload),
                checked(
                    (int)(
                        accessExpiresAt -
                        request.IssuedAt).TotalSeconds));
        }

        private static void ValidateKeyId(
            string keyId,
            string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

            if (keyId.Length > 128 ||
                keyId.Any(char.IsControl))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string SubjectValue(
            IdentityAccess.Domain.SubjectReference subject) =>
            $"{subject.IdentityScopeId:D}:{subject.UserId:D}";

        private string Sign(
            IReadOnlyDictionary<string, object> payload)
        {
            var header =
                new Dictionary<string, object>
                {
                    ["alg"] = "RS256",
                    ["typ"] = "JWT",
                    ["kid"] = activeKeyId
                };

            var encodedHeader =
                OidcBase64Url.Encode(
                    JsonSerializer.SerializeToUtf8Bytes(
                        header));

            var encodedPayload =
                OidcBase64Url.Encode(
                    JsonSerializer.SerializeToUtf8Bytes(
                        payload));

            var signingInput =
                $"{encodedHeader}.{encodedPayload}";

            byte[] signature;

            lock (signingLock)
            {
                signature =
                    activeRsa.SignData(
                        Encoding.ASCII.GetBytes(
                            signingInput),
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
            }

            return $"{signingInput}.{OidcBase64Url.Encode(signature)}";
        }

        private static string AccessTokenHash(
            string accessToken)
        {
            var hash =
                SHA256.HashData(
                    Encoding.ASCII.GetBytes(
                        accessToken));

            return OidcBase64Url.Encode(
                hash.AsSpan(
                    0,
                    hash.Length / 2));
        }

        /// <inheritdoc />
        public void Dispose() =>
            activeRsa.Dispose();
    }
}
