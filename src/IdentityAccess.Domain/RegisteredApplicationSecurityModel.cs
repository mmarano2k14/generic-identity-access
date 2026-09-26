namespace IdentityAccess.Domain
{
    /// <summary>
    /// Durable registered projection of an application-owned security manifest within one identity scope.
    /// </summary>
    public sealed class RegisteredApplicationSecurityModel
    {
        /// <summary>Gets the identity-scoped model reference.</summary>
        public ApplicationSecurityModelReference Reference { get; }

        /// <summary>Gets the normalized application-owned manifest.</summary>
        public ApplicationSecurityManifest Manifest { get; }

        /// <summary>Gets the SHA-256 fingerprint of the normalized manifest semantics.</summary>
        public string ManifestSha256 { get; }

        /// <summary>Gets the concrete capabilities projected for persistence.</summary>
        public IReadOnlyList<ApplicationCapability> Capabilities { get; }

        /// <summary>Initializes a registered application security model.</summary>
        public RegisteredApplicationSecurityModel(
            Guid identityScopeId,
            ApplicationSecurityManifest manifest,
            string manifestSha256)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            if (manifestSha256 is null ||
                manifestSha256.Length != 64 ||
                manifestSha256.Any(character =>
                    (character < '0' || character > '9') &&
                    (character < 'a' || character > 'f')))
            {
                throw new ArgumentException("Manifest SHA-256 must be 64 lowercase hexadecimal characters.", nameof(manifestSha256));
            }

            Reference = new ApplicationSecurityModelReference(identityScopeId, manifest.Application, manifest.ModelVersion);
            Manifest = manifest;
            ManifestSha256 = manifestSha256;
            Capabilities = Array.AsReadOnly(manifest.Capabilities
                .Select(value => new ApplicationCapability(Reference, value.Key, value.DisplayName))
                .ToArray());
        }
    }
}
