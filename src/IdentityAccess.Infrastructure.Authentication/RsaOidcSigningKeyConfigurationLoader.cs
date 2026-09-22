using Microsoft.Extensions.Configuration;

namespace IdentityAccess.Infrastructure.Authentication
{
    /// <summary>Loads and validates trusted server configuration for a process-pinned OIDC RSA key ring.</summary>
    internal static class RsaOidcSigningKeyConfigurationLoader
    {
        /// <summary>Loads either the multi-key ring or the backward-compatible single-key configuration.</summary>
        public static (
            string ActiveSigningKeyId,
            IReadOnlyList<RsaOidcSigningKeyOptions> SigningKeys) Load(
            IConfigurationSection section,
            string contentRootPath)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

            var configuredKeys =
                section.GetSection("SigningKeys")
                    .GetChildren()
                    .ToArray();

            var legacyKeyId =
                section["SigningKeyId"];

            var legacyKeyPath =
                section["SigningKeyPemPath"];

            var activeKeyId =
                section["ActiveSigningKeyId"];

            if (configuredKeys.Length == 0)
            {
                if (!string.IsNullOrWhiteSpace(activeKeyId))
                {
                    throw new InvalidOperationException(
                        "OIDC ActiveSigningKeyId requires the SigningKeys key-ring configuration.");
                }

                if (string.IsNullOrWhiteSpace(legacyKeyId) ||
                    string.IsNullOrWhiteSpace(legacyKeyPath))
                {
                    throw new InvalidOperationException(
                        "OIDC requires either SigningKeys with ActiveSigningKeyId or the legacy SigningKeyId/SigningKeyPemPath pair.");
                }

                return (
                    legacyKeyId,
                    [
                        new RsaOidcSigningKeyOptions(
                            legacyKeyId,
                            ResolvePath(
                                legacyKeyPath,
                                contentRootPath))
                    ]);
            }

            if (!string.IsNullOrWhiteSpace(legacyKeyId) ||
                !string.IsNullOrWhiteSpace(legacyKeyPath))
            {
                throw new InvalidOperationException(
                    "OIDC legacy signing-key fields cannot be combined with SigningKeys.");
            }

            if (string.IsNullOrWhiteSpace(activeKeyId))
            {
                throw new InvalidOperationException(
                    "OIDC ActiveSigningKeyId is required when SigningKeys is configured.");
            }

            var signingKeys =
                new List<RsaOidcSigningKeyOptions>(
                    configuredKeys.Length);

            foreach (var configuredKey in configuredKeys)
            {
                var allowed =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        "KeyId",
                        "PemPath"
                    };

                if (configuredKey.GetChildren().Any(
                        child => !allowed.Contains(child.Key)))
                {
                    throw new InvalidOperationException(
                        "OIDC SigningKeys entry contains an unsupported field.");
                }

                var keyId =
                    configuredKey["KeyId"];

                var pemPath =
                    configuredKey["PemPath"];

                if (string.IsNullOrWhiteSpace(keyId) ||
                    string.IsNullOrWhiteSpace(pemPath))
                {
                    throw new InvalidOperationException(
                        "OIDC SigningKeys entries require KeyId and PemPath.");
                }

                signingKeys.Add(
                    new RsaOidcSigningKeyOptions(
                        keyId,
                        ResolvePath(
                            pemPath,
                            contentRootPath)));
            }

            return (
                activeKeyId,
                signingKeys);
        }

        private static string ResolvePath(
            string configuredKeyPath,
            string contentRootPath)
        {
            try
            {
                return Path.GetFullPath(
                    configuredKeyPath,
                    contentRootPath);
            }
            catch (Exception error) when (
                error is ArgumentException or
                    NotSupportedException or
                    System.Security.SecurityException)
            {
                throw new InvalidOperationException(
                    "OIDC signing-key path is invalid.",
                    error);
            }
        }
    }
}
