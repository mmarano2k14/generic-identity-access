namespace IdentityAccess.Domain
{
    /// <summary>
    /// Versioned application-owned declaration of the RBAC context and concrete capabilities
    /// supported by one application security model.
    /// </summary>
    public sealed class ApplicationSecurityManifest
    {
        private const int SupportedSchemaVersion = 1;

        /// <summary>Gets the manifest schema version.</summary>
        public int SchemaVersion { get; }

        /// <summary>Gets the application key declared by the manifest.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the immutable application security-model version.</summary>
        public int ModelVersion { get; }

        /// <summary>Gets the external RBAC project context.</summary>
        public string RbacProject { get; }

        /// <summary>Gets the allowed external RBAC namespace contexts.</summary>
        public IReadOnlyList<string> RbacNamespaces { get; }

        /// <summary>Gets the concrete capabilities supported by this model.</summary>
        public IReadOnlyList<ApplicationSecurityManifestCapability> Capabilities { get; }

        /// <summary>Initializes a normalized application security manifest.</summary>
        public ApplicationSecurityManifest(
            int schemaVersion,
            ApplicationKey application,
            int modelVersion,
            string rbacProject,
            IEnumerable<string> rbacNamespaces,
            IEnumerable<ApplicationSecurityManifestCapability> capabilities)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(rbacNamespaces);
            ArgumentNullException.ThrowIfNull(capabilities);

            if (schemaVersion != SupportedSchemaVersion)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(schemaVersion),
                    $"Application security manifest schema version {SupportedSchemaVersion} is required.");
            }

            if (modelVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modelVersion));
            }

            var namespaces = rbacNamespaces
                .Select(value => NormalizeRbacContext(value, nameof(rbacNamespaces)))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            if (namespaces.Length == 0)
            {
                throw new ArgumentException("At least one RBAC namespace is required.", nameof(rbacNamespaces));
            }

            var normalizedCapabilities = capabilities
                .OrderBy(value => value.Key.Resource, StringComparer.Ordinal)
                .ThenBy(value => value.Key.Feature, StringComparer.Ordinal)
                .ThenBy(value => value.Key.Action, StringComparer.Ordinal)
                .ToArray();

            if (normalizedCapabilities.Length == 0)
            {
                throw new ArgumentException("At least one concrete capability is required.", nameof(capabilities));
            }

            var duplicateCapability = normalizedCapabilities
                .GroupBy(value => (value.Key.Resource, value.Key.Feature, value.Key.Action))
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateCapability is not null)
            {
                throw new ArgumentException("Duplicate application security capabilities are not allowed.", nameof(capabilities));
            }

            SchemaVersion = schemaVersion;
            Application = application;
            ModelVersion = modelVersion;
            RbacProject = NormalizeRbacContext(rbacProject, nameof(rbacProject));
            RbacNamespaces = Array.AsReadOnly(namespaces);
            Capabilities = Array.AsReadOnly(normalizedCapabilities);
        }

        private static string NormalizeRbacContext(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
            var canonical = value.Trim().ToLowerInvariant();
            if (canonical.Length > 128 ||
                canonical.Contains(':', StringComparison.Ordinal) ||
                canonical.Contains('*', StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "RBAC project and namespace values must be concrete context segments of at most 128 characters.",
                    parameterName);
            }

            return canonical;
        }
    }
}
