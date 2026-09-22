using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityAccess.Application.Authentication;
using IdentityAccess.Domain;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>
    /// Validates self-contained RS256 access tokens against the process-pinned OIDC public key ring
    /// and current trusted authentication-client registry.
    /// </summary>
    internal sealed class RsaOidcAccessTokenValidator :
        IOidcAccessTokenValidator,
        IDisposable
    {
        private const int MaximumEncodedTokenLength = 32768;

        private static readonly string[] HeaderProperties =
        [
            "alg",
            "typ",
            "kid"
        ];

        private static readonly string[] AccessTokenClaims =
        [
            "iss",
            "sub",
            "aud",
            "exp",
            "iat",
            "jti",
            "client_id",
            "scope",
            "sid",
            "identity_scope_id",
            "application_key"
        ];

        private readonly OidcOptions options;
        private readonly IAuthenticationClientRegistry clients;
        private readonly TimeProvider timeProvider;
        private readonly IReadOnlyDictionary<string, RSA> validationKeys;
        private readonly IReadOnlyDictionary<string, object> validationLocks;

        /// <summary>Initializes a process-pinned access-token validator from published OIDC keys.</summary>
        public RsaOidcAccessTokenValidator(
            OidcOptions options,
            IAuthenticationClientRegistry clients,
            IOidcTokenIssuer signingKeySource,
            TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(clients);
            ArgumentNullException.ThrowIfNull(signingKeySource);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.options = options.Validate();
            this.clients = clients;
            this.timeProvider = timeProvider;

            var keys =
                new Dictionary<string, RSA>(
                    StringComparer.Ordinal);

            var locks =
                new Dictionary<string, object>(
                    StringComparer.Ordinal);

            try
            {
                foreach (var signingKey in signingKeySource.SigningKeys)
                {
                    ArgumentNullException.ThrowIfNull(signingKey);
                    ArgumentException.ThrowIfNullOrWhiteSpace(signingKey.KeyId);

                    if (!string.Equals(signingKey.KeyType, "RSA", StringComparison.Ordinal) ||
                        !string.Equals(signingKey.Use, "sig", StringComparison.Ordinal) ||
                        !string.Equals(signingKey.Algorithm, "RS256", StringComparison.Ordinal) ||
                        !OidcBase64Url.TryDecode(signingKey.Modulus, out var modulus) ||
                        !OidcBase64Url.TryDecode(signingKey.Exponent, out var exponent))
                    {
                        throw new InvalidOperationException(
                            "OIDC published signing-key material is not a supported RS256 validation key.");
                    }

                    var rsa = RSA.Create();

                    try
                    {
                        rsa.ImportParameters(
                            new RSAParameters
                            {
                                Modulus = modulus,
                                Exponent = exponent
                            });

                        if (rsa.KeySize < 2048)
                        {
                            throw new InvalidOperationException(
                                "OIDC RSA validation keys must be at least 2048 bits.");
                        }

                        if (!keys.TryAdd(signingKey.KeyId, rsa))
                        {
                            throw new InvalidOperationException(
                                "OIDC access-token validation keys must have unique identifiers.");
                        }

                        locks.Add(
                            signingKey.KeyId,
                            new object());
                    }
                    catch
                    {
                        rsa.Dispose();
                        throw;
                    }
                }

                if (keys.Count == 0)
                {
                    throw new InvalidOperationException(
                        "OIDC access-token validation requires at least one published signing key.");
                }
            }
            catch
            {
                foreach (var key in keys.Values)
                    key.Dispose();

                throw;
            }

            validationKeys = keys;
            validationLocks = locks;
        }

        /// <inheritdoc />
        public OidcAccessTokenValidationResult Validate(
            string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) ||
                accessToken.Length > MaximumEncodedTokenLength)
            {
                return Invalid(
                    OidcAccessTokenValidationFailureCode.MalformedToken);
            }

            var parts = accessToken.Split('.');

            if (parts.Length != 3 ||
                parts.Any(string.IsNullOrEmpty) ||
                !OidcBase64Url.TryDecode(parts[0], out var headerBytes) ||
                !OidcBase64Url.TryDecode(parts[1], out var payloadBytes) ||
                !OidcBase64Url.TryDecode(parts[2], out var signature))
            {
                return Invalid(
                    OidcAccessTokenValidationFailureCode.MalformedToken);
            }

            string keyId;

            try
            {
                using var header =
                    JsonDocument.Parse(
                        headerBytes,
                        JsonOptions());

                if (!HasExactProperties(
                        header.RootElement,
                        HeaderProperties) ||
                    !TryGetString(header.RootElement, "alg", out var algorithm) ||
                    !TryGetString(header.RootElement, "typ", out var tokenType) ||
                    !TryGetString(header.RootElement, "kid", out keyId))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.MalformedToken);
                }

                if (!string.Equals(algorithm, "RS256", StringComparison.Ordinal) ||
                    !string.Equals(tokenType, "JWT", StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(keyId) ||
                    keyId.Length > 128 ||
                    keyId.Any(char.IsControl))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.UnsupportedHeader);
                }
            }
            catch (JsonException)
            {
                return Invalid(
                    OidcAccessTokenValidationFailureCode.MalformedToken);
            }

            if (!validationKeys.TryGetValue(keyId, out var validationKey) ||
                !validationLocks.TryGetValue(keyId, out var validationLock))
            {
                return Invalid(
                    OidcAccessTokenValidationFailureCode.UnknownSigningKey);
            }

            var signingInput =
                Encoding.ASCII.GetBytes(
                    $"{parts[0]}.{parts[1]}");

            bool signatureValid;

            try
            {
                lock (validationLock)
                {
                    signatureValid =
                        validationKey.VerifyData(
                            signingInput,
                            signature,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1);
                }
            }
            catch (CryptographicException)
            {
                signatureValid = false;
            }

            if (!signatureValid)
            {
                return Invalid(
                    OidcAccessTokenValidationFailureCode.SignatureInvalid);
            }

            try
            {
                using var payload =
                    JsonDocument.Parse(
                        payloadBytes,
                        JsonOptions());

                if (!HasExactProperties(
                        payload.RootElement,
                        AccessTokenClaims))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClaimsInvalid);
                }

                if (!TryGetString(payload.RootElement, "iss", out var issuer) ||
                    !string.Equals(issuer, options.CanonicalIssuer, StringComparison.Ordinal))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.IssuerMismatch);
                }

                if (!TryGetString(payload.RootElement, "aud", out var audience) ||
                    !string.Equals(audience, options.AccessTokenAudience, StringComparison.Ordinal))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.AudienceMismatch);
                }

                if (!TryGetInt64(payload.RootElement, "exp", out var expiresUnix) ||
                    !TryGetInt64(payload.RootElement, "iat", out var issuedUnix))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClaimsInvalid);
                }

                DateTimeOffset issuedAt;
                DateTimeOffset expiresAt;

                try
                {
                    issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedUnix);
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.LifetimeInvalid);
                }

                var now = timeProvider.GetUtcNow();

                if (expiresAt <= now)
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.Expired);
                }

                var maximumLifetime =
                    TimeSpan.FromMinutes(
                        options.AccessTokenLifetimeMinutes);

                if (issuedAt > now ||
                    expiresAt <= issuedAt ||
                    expiresAt - issuedAt > maximumLifetime)
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.LifetimeInvalid);
                }

                if (!TryGetString(payload.RootElement, "client_id", out var clientId) ||
                    !TryGetString(payload.RootElement, "scope", out var scope) ||
                    !clients.TryGet(clientId, out var client) ||
                    !client.OidcEnabled ||
                    !client.AllowsOidcScope(scope))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClientBindingInvalid);
                }

                if (!TryGetString(payload.RootElement, "application_key", out var applicationValue))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClaimsInvalid);
                }

                ApplicationKey application;

                try
                {
                    application = new ApplicationKey(applicationValue);
                }
                catch (ArgumentException)
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClaimsInvalid);
                }

                if (application != client.Application)
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClientBindingInvalid);
                }

                if (!TryGetString(payload.RootElement, "sid", out var sessionValue) ||
                    !Guid.TryParseExact(sessionValue, "D", out var sessionId) ||
                    sessionId == Guid.Empty ||
                    !TryGetString(payload.RootElement, "identity_scope_id", out var identityScopeValue) ||
                    !Guid.TryParseExact(identityScopeValue, "D", out var identityScopeId) ||
                    identityScopeId == Guid.Empty ||
                    !TryGetString(payload.RootElement, "jti", out var tokenValue) ||
                    !Guid.TryParseExact(tokenValue, "N", out var tokenId) ||
                    tokenId == Guid.Empty ||
                    !TryGetString(payload.RootElement, "sub", out var subjectValue) ||
                    !TryReadSubject(subjectValue, identityScopeId, out var subject))
                {
                    return Invalid(
                        OidcAccessTokenValidationFailureCode.ClaimsInvalid);
                }

                return OidcAccessTokenValidationResult.Success(
                    new ValidatedOidcAccessToken(
                        subject,
                        sessionId,
                        client.ClientId,
                        client.Application,
                        client.AuthenticationContextKey,
                        scope,
                        tokenId,
                        issuedAt,
                        expiresAt));
            }
            catch (JsonException)
            {
                return Invalid(
                    OidcAccessTokenValidationFailureCode.MalformedToken);
            }
        }

        private static JsonDocumentOptions JsonOptions() =>
            new()
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 8
            };

        private static bool HasExactProperties(
            JsonElement element,
            IReadOnlyCollection<string> expected)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return false;

            var observed =
                new HashSet<string>(
                    StringComparer.Ordinal);

            foreach (var property in element.EnumerateObject())
            {
                if (!observed.Add(property.Name))
                    return false;
            }

            return observed.Count == expected.Count &&
                expected.All(observed.Contains);
        }

        private static bool TryGetString(
            JsonElement element,
            string propertyName,
            out string value)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String &&
                property.GetString() is { } parsed &&
                !string.IsNullOrWhiteSpace(parsed) &&
                !parsed.Any(char.IsControl))
            {
                value = parsed;
                return true;
            }

            value = string.Empty;
            return false;
        }

        private static bool TryGetInt64(
            JsonElement element,
            string propertyName,
            out long value)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt64(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadSubject(
            string value,
            Guid expectedIdentityScopeId,
            out SubjectReference subject)
        {
            var separator = value.IndexOf(':');

            if (separator <= 0 ||
                separator != value.LastIndexOf(':') ||
                !Guid.TryParseExact(value[..separator], "D", out var identityScopeId) ||
                !Guid.TryParseExact(value[(separator + 1)..], "D", out var userId) ||
                identityScopeId == Guid.Empty ||
                userId == Guid.Empty ||
                identityScopeId != expectedIdentityScopeId ||
                !string.Equals(
                    value,
                    $"{identityScopeId:D}:{userId:D}",
                    StringComparison.Ordinal))
            {
                subject = null!;
                return false;
            }

            subject =
                new SubjectReference(
                    identityScopeId,
                    userId);

            return true;
        }

        private static OidcAccessTokenValidationResult Invalid(
            OidcAccessTokenValidationFailureCode failureCode) =>
            OidcAccessTokenValidationResult.Invalid(
                failureCode);

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var key in validationKeys.Values)
                key.Dispose();
        }
    }
}
